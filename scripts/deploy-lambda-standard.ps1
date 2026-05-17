<#
.SYNOPSIS
Simple SAM deploy script: builds, deploys, and verifies Lambda env vars.

.DESCRIPTION
This script runs `sam build`, deploys the built template with `sam deploy`, and
verifies that the Lambda functions in the stack have the provided TOKEN values.

Usage examples:
PowerShell:
  .\scripts\deploy-lambda-standard.ps1 -ProfileName tbirdcontractinggmailcom

#>
param(
    [string]$StackName = 'webapplicationarch',
    [string]$S3Bucket = 'webapparchdeploy',
    [string]$TemplatePath = 'api/WebApplicationArch/serverless.template',
    [string]$ProjectPath = 'api/WebApplicationArch',
    [string]$ProfileName = '',
    [string]$Region = 'us-west-2',
    [switch]$RotateTokens
)

$repoRoot = Resolve-Path "$PSScriptRoot\.."
$projectFullPath = Resolve-Path "$repoRoot\$ProjectPath"
$templateFullPath = Resolve-Path "$repoRoot\$TemplatePath"

function Ensure-Command($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        Write-Error "$name is not installed or not available in PATH."
        exit 1
    }
}

Ensure-Command sam
Ensure-Command aws

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

Write-Host "Using AWS profile: $ProfileName  region: $Region" -ForegroundColor Yellow
& aws sts get-caller-identity --profile $ProfileName | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "AWS profile validation failed for profile '$ProfileName'."
    exit $LASTEXITCODE
}

if ($RotateTokens) {
    $tokenSecret = [System.Guid]::NewGuid().ToString().ToUpper()
    $tokenIV = -join ((65..90) + (97..122) | Get-Random -Count 16 | ForEach-Object { [char]$_ })
} else {
    $tokenSecret = $env:TOKEN_SECRET
    $tokenIV = $env:TOKEN_IV
    if ([string]::IsNullOrEmpty($tokenSecret)) { $tokenSecret = Read-Host 'Enter TOKEN_SECRET' }
    if ([string]::IsNullOrEmpty($tokenIV)) { $tokenIV = Read-Host 'Enter TOKEN_IV (16 chars)' }
}

Write-Host 'Deploy settings:' -ForegroundColor Cyan
Write-Host "  StackName = $StackName"
Write-Host "  S3Bucket  = $S3Bucket"
Write-Host "  Profile   = $ProfileName"
Write-Host "  Region    = $Region"
Write-Host "  Rotate    = $RotateTokens"
Write-Host ''

$confirmation = Read-Host 'Proceed with build and deploy? (Y/N)'
if ($confirmation -notin @('Y','y','Yes','yes')) { Write-Host 'Cancelled.'; exit 0 }

$env:AWS_PROFILE = $ProfileName
$env:AWS_REGION = $Region
$env:AWS_DEFAULT_REGION = $Region

Push-Location $projectFullPath
try {
    Write-Host 'Building SAM application...' -ForegroundColor Green
    & sam build --template-file $templateFullPath --build-dir .aws-sam\build
    if ($LASTEXITCODE -ne 0) { throw 'sam build failed.' }

    $builtTemplate = Join-Path (Resolve-Path '.aws-sam\build') 'template.yaml'

    Write-Host 'Deploying stack...' -ForegroundColor Green
    & sam deploy --template-file $builtTemplate --stack-name $StackName --s3-bucket $S3Bucket --capabilities CAPABILITY_IAM --parameter-overrides "TokenSecret=$tokenSecret" "TokenIV=$tokenIV" --profile $ProfileName --region $Region
    if ($LASTEXITCODE -ne 0) { throw 'sam deploy failed.' }

    Write-Host 'Verifying deployed Lambda environment variables...' -ForegroundColor Green
    $lambdaFunctionsJson = & aws cloudformation list-stack-resources --stack-name $StackName --profile $ProfileName --region $Region --query "StackResourceSummaries[?ResourceType=='AWS::Lambda::Function'].PhysicalResourceId" --output json
    $lambdaFunctions = $lambdaFunctionsJson | ConvertFrom-Json
    if (-not $lambdaFunctions -or $lambdaFunctions.Count -eq 0) { Write-Warning 'No Lambda functions found in stack.' } else {
        $firstSecret = $null
        $firstIV = $null
        $allMatch = $true
        foreach ($f in $lambdaFunctions) {
            $name = $f.Trim()
            $cfg = & aws lambda get-function-configuration --function-name $name --profile $ProfileName --region $Region --query '{FunctionName:FunctionName, TOKEN_SECRET:Environment.Variables.TOKEN_SECRET, TOKEN_IV:Environment.Variables.TOKEN_IV}' --output json
            $obj = $cfg | ConvertFrom-Json
            if ($null -eq $firstSecret) {
                $firstSecret = $obj.TOKEN_SECRET
                $firstIV = $obj.TOKEN_IV
            } else {
                if ($obj.TOKEN_SECRET -ne $firstSecret -or $obj.TOKEN_IV -ne $firstIV) {
                    $allMatch = $false
                    Write-Host "Function: $($obj.FunctionName) - Token MISMATCH" -ForegroundColor Red
                    continue
                }
            }
            Write-Host "Function: $($obj.FunctionName) - Tokens OK" -ForegroundColor Cyan
        }
        if ($allMatch) { Write-Host 'All functions have matching TOKEN_SECRET and TOKEN_IV (values hidden).' -ForegroundColor Green } else { Write-Error 'Token mismatch detected across functions.' }
    }
}
catch {
    Write-Error $_.Exception.Message
    exit 1
}
finally { Pop-Location }

Write-Host 'Deployment finished.' -ForegroundColor Green
