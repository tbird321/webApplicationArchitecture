<#
.SYNOPSIS
Deploys the Lambda stack without generating new TOKEN_SECRET/TOKEN_IV values.

.DESCRIPTION
This script deploys the backend stack using the existing TOKEN_SECRET and TOKEN_IV.
It reads values from environment variables if available, otherwise prompts the user.

.PARAMETER StackName
The CloudFormation stack name. Default: webapplicationarch

.PARAMETER S3Bucket
The S3 bucket used for SAM deployment. Default: webapparchdeploy

.PARAMETER TemplatePath
The path to the SAM template, relative to the repo root.

.PARAMETER ProjectPath
The path to the Lambda project folder, relative to the repo root.

.PARAMETER SkipPublishSitesCheck
Skip the guard that refuses to silently shrink STATIC_PUBLISH_SITES. Only for the case
where you deliberately intend to turn static publishing off for a site.
#>
param(
    [string]$StackName = 'webapplicationarch',
    [string]$S3Bucket = 'webapparchdeploy',
    [string]$TemplatePath = 'api/WebApplicationArch/serverless.template',
    [string]$ProjectPath = 'api/WebApplicationArch',
    [string]$ProfileName = '',
    [string]$Region = 'us-west-2',
    [switch]$SkipPublishSitesCheck
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
# Set region env vars BEFORE the sts call so profiles that don't carry a default region still validate.
if (-not [string]::IsNullOrEmpty($Region)) {
    $env:AWS_REGION = $Region
    $env:AWS_DEFAULT_REGION = $Region
}
& aws sts get-caller-identity --profile $ProfileName --region $Region | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Error "AWS profile validation failed for profile '$ProfileName'. Check your credentials and try again."
    exit $LASTEXITCODE
}

$tokenSecret = $env:TOKEN_SECRET
$tokenIV     = $env:TOKEN_IV

if ([string]::IsNullOrEmpty($tokenSecret)) {
    $tokenSecret = Read-Host 'Enter current TOKEN_SECRET value (existing value from deployed Lambda)'
}

if ([string]::IsNullOrEmpty($tokenIV)) {
    $tokenIV = Read-Host 'Enter current TOKEN_IV value (existing value from deployed Lambda)'
}

if ([string]::IsNullOrEmpty($tokenSecret) -or [string]::IsNullOrEmpty($tokenIV)) {
    Write-Error 'TOKEN_SECRET and TOKEN_IV must be provided to deploy the template.'
    exit 1
}

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

Write-Host 'Using existing token values (hidden).' -ForegroundColor Cyan
Write-Host ''
# Which sites the on-save hook may publish static pages for. EMPTY = publish nothing,
# which is the safe default: publishing to a site that has not been migrated would write
# a page named "Home" straight over its live SPA shell at the bucket root. Opt sites in
# one at a time (comma-separated website ids, or 'all') as they are cut over.
# NOTE: this is a template parameter, so it is re-applied on EVERY deploy -- if it is not
# set here, a later deploy silently turns the hook back off.
# Managed policy that grants the Lambdas read access to the CMS database credentials in
# Secrets Manager. The API functions already carry it; the S3 render trigger needs it too or
# it cannot resolve which pages to re-render. Override via RDS_SECRET_POLICY_ARN when moving
# to a different AWS account.
$rdsSecretPolicyArn = $env:RDS_SECRET_POLICY_ARN
if ([string]::IsNullOrWhiteSpace($rdsSecretPolicyArn)) {
    $rdsSecretPolicyArn = 'arn:aws:iam::646797148861:policy/webapplicationRDSSecretRead'
}

# Resolve from the shell first, then fall back to the User-scoped variable. The fallback is
# not a nicety: a shell opened BEFORE the User variable was set inherits nothing, so the
# obvious-looking deploy from that shell would pass StaticPublishSites='' and silently turn
# static publishing off for EVERY site -- including ones migrated weeks ago, which would then
# quietly stop updating with no error anywhere.
$staticPublishSites = $env:STATIC_PUBLISH_SITES
if ([string]::IsNullOrWhiteSpace($staticPublishSites)) {
    $userScoped = [Environment]::GetEnvironmentVariable('STATIC_PUBLISH_SITES', 'User')
    if (-not [string]::IsNullOrWhiteSpace($userScoped)) {
        $staticPublishSites = $userScoped
        Write-Host "STATIC_PUBLISH_SITES not set in this shell; using the User-scoped value '$userScoped'." -ForegroundColor Yellow
    }
}
if ($null -eq $staticPublishSites) { $staticPublishSites = '' }

# Deploying an empty value is a legitimate thing to want (disable everything deliberately),
# but it must never happen by omission. Compare against what is already deployed and make the
# operator say yes to a reduction.
if (-not $SkipPublishSitesCheck) {
    $deployedSites = ''
    $q = 'Stacks[0].Parameters[?ParameterKey==`StaticPublishSites`].ParameterValue'
    $describeArgs = @('cloudformation', 'describe-stacks', '--stack-name', $StackName,
                      '--region', $Region, '--query', $q, '--output', 'text')
    if (-not [string]::IsNullOrEmpty($ProfileName)) { $describeArgs += @('--profile', $ProfileName) }
    # A first-ever deploy has no stack to read, which is fine -- there is nothing to lose yet.
    $raw = & aws @describeArgs 2>$null
    if ($LASTEXITCODE -eq 0 -and $raw) { $deployedSites = ($raw -join '').Trim() }
    if ($deployedSites -eq 'None') { $deployedSites = '' }

    $currentIds = @($staticPublishSites -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $deployedIds = @($deployedSites -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
    $losing = @($deployedIds | Where-Object { $_ -notin $currentIds -and $currentIds -notcontains 'all' })

    if ($losing.Count -gt 0) {
        Write-Host ''
        Write-Host 'WARNING: this deploy would STOP static publishing for site(s) that have it today.' -ForegroundColor Red
        Write-Host ("  currently deployed : '{0}'" -f $deployedSites) -ForegroundColor Red
        Write-Host ("  about to deploy    : '{0}'" -f $staticPublishSites) -ForegroundColor Red
        Write-Host ("  would lose         : {0}" -f ($losing -join ', ')) -ForegroundColor Red
        Write-Host ''
        Write-Host 'Those sites keep serving, but every future content edit stops reaching the' -ForegroundColor DarkGray
        Write-Host 'public page, with no error. The list is cumulative -- include every migrated site.' -ForegroundColor DarkGray
        Write-Host ''
        $answer = Read-Host "Continue anyway? (y/N)"
        if ($answer -ne 'y') {
            Write-Host 'Aborted. Set the full list and re-run, e.g.:' -ForegroundColor Yellow
            Write-Host ("  `$env:STATIC_PUBLISH_SITES = '{0}'" -f (($deployedIds + $currentIds | Select-Object -Unique) -join ','))
            exit 1
        }
    }
}
Write-Host ("STATIC_PUBLISH_SITES = '{0}'{1}" -f $staticPublishSites,
    $(if ([string]::IsNullOrWhiteSpace($staticPublishSites)) { '  (on-save static publishing DISABLED)' } else { '' })) -ForegroundColor Cyan
Write-Host ''
Write-Host 'Deploy settings:' -ForegroundColor Cyan
Write-Host "  StackName   = $StackName"
Write-Host "  S3Bucket    = $S3Bucket"
Write-Host "  Template    = $templateFullPath"
Write-Host "  ProjectDir  = $projectFullPath"
Write-Host "  ProfileName = $ProfileName"
Write-Host "  Region      = $Region"
Write-Host "  PublishSites= '$staticPublishSites'"
Write-Host ''

$confirmation = Read-Host 'Proceed with deploy using these values? (Y/N)'
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

    Write-Host 'Deploying stack without rotating secrets...' -ForegroundColor Green
    $deployOut = & sam deploy --region $Region --template-file $builtTemplatePath @profileArg --stack-name $StackName --s3-bucket $S3Bucket --capabilities CAPABILITY_IAM --parameter-overrides "TokenSecret=$tokenSecret" "TokenIV=$tokenIV" "McpApiKey=$mcpApiKey" "LambdaApiBaseUrl=$lambdaApiBaseUrl" "ContentBucket=www-websitecontent" "StaticPublishSites=$staticPublishSites" "RdsSecretReadPolicyArn=$rdsSecretPolicyArn" 2>&1
    $deployExit = $LASTEXITCODE
    $deployOut | ForEach-Object { Write-Host $_ }

    # SAM exits non-zero when the changeset is empty. That is not a failure -- it means
    # the stack already matches the template, which is the expected result of re-running
    # a deploy. Treating it as an error makes a successful no-op look like a broken one.
    $noChanges = ($deployOut | Out-String) -match 'No changes to deploy'
    if ($deployExit -ne 0 -and -not $noChanges) {
        throw "SAM deploy failed with exit code $deployExit."
    }
    if ($noChanges) {
        Write-Host 'Stack already matches the template -- nothing to deploy.' -ForegroundColor Cyan
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
