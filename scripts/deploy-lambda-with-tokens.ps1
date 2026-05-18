<#
.SYNOPSIS
Deploys the Lambda stack with fresh TOKEN_SECRET and TOKEN_IV values.

.DESCRIPTION
This script generates a new GUID-based TOKEN_SECRET and a 16-character TOKEN_IV,
validates the SAM template, and deploys the backend using the existing repo defaults.

.PARAMETER StackName
The CloudFormation stack name. Default: webapplicationarch

.PARAMETER S3Bucket
The S3 bucket used for SAM deployment. Default: webapparchdeploy

.PARAMETER TemplatePath
The path to the SAM template, relative to the repo root.

.PARAMETER ProjectPath
The path to the Lambda project folder, relative to the repo root.
#>
param(
    [string]$StackName = 'webapplicationarch',
    [string]$S3Bucket = 'webapparchdeploy',
    [string]$TemplatePath = 'api/WebApplicationArch/serverless.template',
    [string]$ProjectPath = 'api/WebApplicationArch',
    [string]$ProfileName = '',
    [string]$Region = 'us-west-2'
)

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$projectFullPath = Resolve-Path "$repoRoot\$ProjectPath"
$templateFullPath = Resolve-Path "$repoRoot\$TemplatePath"

if (-not (Get-Command sam -ErrorAction SilentlyContinue)) {
    Write-Error 'SAM CLI is not installed or not available in PATH. Install SAM CLI before running this script.'
    exit 1
}

if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
    Write-Error 'AWS CLI is not installed or not available in PATH. Install AWS CLI before running this script.'
    exit 1
}

if ([string]::IsNullOrEmpty($ProfileName)) {
    $ProfileName = $env:AWS_PROFILE
}

if ([string]::IsNullOrEmpty($ProfileName)) {
    $ProfileName = Read-Host 'Enter AWS profile name to use for deploy'
}

if ([string]::IsNullOrEmpty($ProfileName)) {
    Write-Error 'AWS profile must be provided via -ProfileName or AWS_PROFILE.'
    exit 1
}

Write-Host "Verifying AWS profile: $ProfileName" -ForegroundColor Yellow
& aws sts get-caller-identity --profile $ProfileName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "AWS profile validation failed for profile '$ProfileName'. Check your credentials and try again."
    exit $LASTEXITCODE
}

$tokenSecret = [System.Guid]::NewGuid().ToString().ToUpper()
$tokenIV = -join ((65..90) + (97..122) | Get-Random -Count 16 | ForEach-Object { [char]$_ })

$mcpApiKey = $env:MCP_API_KEY
if ([string]::IsNullOrEmpty($mcpApiKey)) { $mcpApiKey = Read-Host 'Enter MCP_API_KEY' }
if ([string]::IsNullOrEmpty($mcpApiKey)) {
    Write-Error 'MCP_API_KEY must be provided.'
    exit 1
}

$lambdaApiBaseUrl = $env:LAMBDA_API_BASE_URL
if ([string]::IsNullOrEmpty($lambdaApiBaseUrl)) { $lambdaApiBaseUrl = Read-Host 'Enter LAMBDA_API_BASE_URL (e.g. https://abc123.execute-api.us-west-2.amazonaws.com/prod)' }
if ([string]::IsNullOrEmpty($lambdaApiBaseUrl)) {
    Write-Error 'LAMBDA_API_BASE_URL must be provided.'
    exit 1
}

Write-Host 'Generated new token values (hidden).' -ForegroundColor Cyan
Write-Host ''
Write-Host 'Deploy settings:' -ForegroundColor Cyan
Write-Host "  StackName   = $StackName"
Write-Host "  S3Bucket    = $S3Bucket"
Write-Host "  Template    = $templateFullPath"
Write-Host "  ProjectDir  = $projectFullPath"
Write-Host "  ProfileName = $ProfileName"
Write-Host "  Region      = $Region"
Write-Host ''

$confirmation = Read-Host 'Proceed with deploy? (Y/N)'
if ($confirmation -notin @('Y','y','Yes','yes')) {
    Write-Host 'Deployment cancelled.'
    exit 0
}

$profileArg = @()
if (-not [string]::IsNullOrEmpty($ProfileName)) {
    Write-Host "Using AWS profile: $ProfileName" -ForegroundColor Yellow
    $env:AWS_PROFILE = $ProfileName
    $profileArg += '--profile'
    $profileArg += $ProfileName
}
if (-not [string]::IsNullOrEmpty($Region)) {
    $env:AWS_REGION = $Region
    $env:AWS_DEFAULT_REGION = $Region
}

Push-Location $projectFullPath
try {
    Write-Host 'Validating SAM template...' -ForegroundColor Green
    & sam validate --template-file $templateFullPath
    if ($LASTEXITCODE -ne 0) {
        throw "SAM template validation failed with exit code $LASTEXITCODE."
    }

    $buildDir = Join-Path $projectFullPath '.aws-sam\build'
    $builtTemplatePath = Join-Path $buildDir 'template.yaml'

    Write-Host 'Building SAM application...' -ForegroundColor Green
    & sam build --template-file $templateFullPath --build-dir $buildDir
    if ($LASTEXITCODE -ne 0) {
        throw "SAM build failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Deploying stack...' -ForegroundColor Green
    & sam deploy --region $Region --template-file $builtTemplatePath @profileArg --stack-name $StackName --s3-bucket $S3Bucket --capabilities CAPABILITY_IAM --parameter-overrides "TokenSecret=$tokenSecret" "TokenIV=$tokenIV" "McpApiKey=$mcpApiKey" "LambdaApiBaseUrl=$lambdaApiBaseUrl" "ContentBucket=www-websitecontent"
    if ($LASTEXITCODE -ne 0) {
        throw "SAM deploy failed with exit code $LASTEXITCODE."
    }

    Write-Host 'Verifying deployed Lambda environment variables...' -ForegroundColor Green
    $lambdaFunctionsJson = & aws cloudformation list-stack-resources --stack-name $StackName --profile $ProfileName --region $Region --query "StackResourceSummaries[?ResourceType=='AWS::Lambda::Function'].PhysicalResourceId" --output json
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to list stack resources for stack '$StackName'."
    }

    $lambdaFunctions = $lambdaFunctionsJson | ConvertFrom-Json
    if (-not $lambdaFunctions -or $lambdaFunctions.Count -eq 0) {
        throw "No Lambda functions were found in stack '$StackName'."
    }

    foreach ($functionName in $lambdaFunctions) {
        $functionName = $functionName.Trim()
        $envJson = & aws lambda get-function-configuration --function-name $functionName --profile $ProfileName --region $Region --query "{TOKEN_SECRET:Environment.Variables.TOKEN_SECRET, TOKEN_IV:Environment.Variables.TOKEN_IV}" --output json
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to retrieve configuration for Lambda function '$functionName'."
        }

        $env = $envJson | ConvertFrom-Json
        if ($env.TOKEN_SECRET -ne $tokenSecret -or $env.TOKEN_IV -ne $tokenIV) {
            throw "Environment variable verification failed for '$functionName' (token mismatch)."
        }

        Write-Host "  Verified env vars on $functionName" -ForegroundColor Cyan
    }
}
catch {
    Write-Error $_.Exception.Message
    exit $LASTEXITCODE
}
finally {
    Pop-Location
}

Write-Host 'Deployment finished.' -ForegroundColor Green
Write-Host 'Keep the generated TOKEN_SECRET and TOKEN_IV values secure.'
