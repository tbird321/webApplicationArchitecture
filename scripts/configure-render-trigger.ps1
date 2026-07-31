<#
.SYNOPSIS
    Wire the content bucket's article uploads to the static re-render Lambda.

.DESCRIPTION
    The React admin does not save article content through the API -- it writes straight to
    s3://www-websitecontent/public/websites/{domain}/articles/{path} using Amplify Storage
    with Cognito credentials (FileProcessing.saveFileData -> Storage.put). The MCP tools and
    the PowerShell scripts write there too.

    So the re-render cannot hang off an API endpoint: it has to be driven by the BUCKET, or
    a normal edit updates the article and silently leaves the public page stale.

    This adds an S3 ObjectCreated notification, scoped to the articles/ prefix, targeting
    StaticRenderOnUploadFunction. The bucket is not managed by the CloudFormation stack, so
    SAM cannot attach this -- hence a separate script.

    IDEMPOTENT and NON-DESTRUCTIVE. S3 replaces the WHOLE notification configuration on every
    PUT, so this reads the existing config, removes only its own entry by id, appends the
    current one, and writes the result back. Any other notification on the bucket survives.

.PARAMETER StackName
    CloudFormation stack holding the Lambda. Default: webapplicationarch

.PARAMETER ContentBucket
    Bucket the admin writes article HTML into. Default: www-websitecontent

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom

.PARAMETER Remove
    Delete this notification and leave any others in place. The rollback switch.

.PARAMETER WhatIf
    Show the configuration that would be written without writing it.

.EXAMPLE
    ./scripts/configure-render-trigger.ps1

.EXAMPLE
    ./scripts/configure-render-trigger.ps1 -WhatIf

.EXAMPLE
    ./scripts/configure-render-trigger.ps1 -Remove
#>

[CmdletBinding()]
param(
    [string]$StackName     = 'webapplicationarch',
    [string]$ContentBucket = 'www-websitecontent',
    [string]$AwsProfile    = 'tbirdcontractinggmailcom',
    [string]$Region        = 'us-west-2',
    [switch]$Remove,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"

# Every notification this script manages, with a stable id so re-running REPLACES our entries
# rather than stacking duplicates. Anything on the bucket that is not in this table is left
# alone -- so an entry added by hand survives, which is exactly how the environment drifted
# from the repo once already.
#
# S3 filter rules allow ONE prefix and ONE suffix per entry and no wildcard in the middle, so
# each distinct file shape needs its own entry: '.html' never matches 'sitemenu.json', and
# theme.css is not even under the same prefix.
#
# All of them target the SAME Lambda; the parsers in ApiStaticRenderFunctions tell them apart
# and decide whether the write is a one-page render or a whole-site one.
$Prefix = 'public/websites/'

$OurEntries = @(
    # Article HTML -> re-render the pages that embed it. The broad prefix is unavoidable
    # (no mid-key wildcard), so TryParseArticleKey does the real filtering and rejects
    # everything that is not under articles/.
    #
    # NOTE: this entry also delivers header.html, which is what makes the header re-render
    # work with no notification of its own -- TryParseSiteAssetKey picks it up.
    @{ Id = 'static-render-on-article-upload'; Prefix = $Prefix;          Suffix = '.html' }

    # The nav. Without this a menu edit updates sitemenu.json and every already-rendered page
    # keeps the old nav -- the new item is unreachable and nothing reports a problem.
    @{ Id = 'static-render-on-menu-upload';    Prefix = $Prefix;          Suffix = 'sitemenu.json' }

    # Public title + GA measurement id, written into every page's <head>.
    @{ Id = 'static-render-on-sitemeta-upload'; Prefix = $Prefix;         Suffix = 'site-meta.json' }

    # ThemeBuilder output, inlined into every page. Different prefix entirely:
    # public/assets/{domain}/themes/theme.css.
    @{ Id = 'static-render-on-theme-upload';   Prefix = 'public/assets/'; Suffix = 'theme.css' }
)

Write-Host 'Static re-render trigger' -ForegroundColor White
Write-Host "  bucket  : s3://$ContentBucket"
Write-Host "  entries : $($OurEntries.Count)"
foreach ($e in $OurEntries) { Write-Host ("            {0}*{1}" -f $e.Prefix, $e.Suffix) }
Write-Host "  profile : $AwsProfile"

Test-AwsAuth -AwsProfile $AwsProfile -Region $Region | Out-Null
$workDir = Join-Path ([System.IO.Path]::GetTempPath()) 'render-trigger'

# ---------------------------------------------------------------- locate the Lambda
Write-Step 1 'Locate the render Lambda'
$fnName = Invoke-Aws @('cloudformation', 'describe-stacks', '--stack-name', $StackName,
    '--profile', $AwsProfile, '--region', $Region,
    '--query', "Stacks[0].Outputs[?OutputKey=='StaticRenderFunctionName'].OutputValue",
    '--output', 'text')

if (-not $fnName -or $fnName -eq 'None') {
    throw @"
The stack has no StaticRenderFunctionName output, which means the render Lambda has not been
deployed yet. Deploy first, then re-run:

    ./scripts/deploy-lambda-no-rotate.ps1 -ProfileName $AwsProfile
"@
}
$fnName = "$fnName".Trim()
$fnArn = Invoke-Aws @('lambda', 'get-function-configuration', '--function-name', $fnName,
    '--profile', $AwsProfile, '--region', $Region, '--query', 'FunctionArn', '--output', 'text')
Write-Ok "$fnName"
Write-Note "$fnArn"

# ------------------------------------------------- read the existing notification config
Write-Step 2 'Read the current bucket notification configuration'
$existing = Invoke-Aws @('s3api', 'get-bucket-notification-configuration', '--bucket', $ContentBucket,
    '--profile', $AwsProfile, '--region', $Region, '--output', 'json')

# An unconfigured bucket returns an empty object, so Get-Prop yields $null. Two PowerShell
# traps compound here:
#   @($null)      is an array containing ONE null element, not an empty array. Serialised,
#                 that becomes [null] and S3 rejects the whole request
#                 ("Invalid type for parameter ... value: None").
#   return ,@()   does NOT reliably survive a function boundary for an EMPTY array -- the
#                 caller ends up with [[]], an array containing an empty array.
# Both are avoided by filtering inline, with no helper function in the way.
$lambdaCfgs = @()
$topicCfgs  = @()
$queueCfgs  = @()

$raw = Get-Prop $existing 'LambdaFunctionConfigurations'
if ($null -ne $raw) { $lambdaCfgs = @($raw | Where-Object { $null -ne $_ }) }

$raw = Get-Prop $existing 'TopicConfigurations'
if ($null -ne $raw) { $topicCfgs = @($raw | Where-Object { $null -ne $_ }) }

$raw = Get-Prop $existing 'QueueConfigurations'
if ($null -ne $raw) { $queueCfgs = @($raw | Where-Object { $null -ne $_ }) }

Write-Note ("existing: {0} lambda, {1} topic, {2} queue configuration(s)" -f
    $lambdaCfgs.Count, $topicCfgs.Count, $queueCfgs.Count)
foreach ($c in $lambdaCfgs) {
    if ($c) { Write-Note ("  - {0} -> {1}" -f (Get-Prop $c 'Id'), (Get-Prop $c 'LambdaFunctionArn')) }
}

# Drop only our own entries; everything else is preserved verbatim.
$ourIds = @($OurEntries | ForEach-Object { $_.Id })
$kept = @($lambdaCfgs | Where-Object { $ourIds -notcontains (Get-Prop $_ 'Id') })
if ($kept.Count -ne $lambdaCfgs.Count) { Write-Note "replacing $($lambdaCfgs.Count - $kept.Count) existing entry/entries of ours" }

# ------------------------------------------------------------------- build the new config
Write-Step 3 $(if ($Remove) { 'Remove the triggers' } else { 'Add the triggers' })

$newLambdaCfgs = @($kept)
if (-not $Remove) {
    foreach ($e in $OurEntries) {
        Write-Note ("  + {0}  ({1}*{2})" -f $e.Id, $e.Prefix, $e.Suffix)
        $newLambdaCfgs += [pscustomobject]@{
            Id                = $e.Id
            LambdaFunctionArn = $fnArn
            Events            = @('s3:ObjectCreated:*')
            Filter            = [pscustomobject]@{
                Key = [pscustomobject]@{
                    FilterRules = @(
                        [pscustomobject]@{ Name = 'prefix'; Value = $e.Prefix }
                        [pscustomobject]@{ Name = 'suffix'; Value = $e.Suffix }
                    )
                }
            }
        }
    }
}

$config = [ordered]@{}
if ($newLambdaCfgs.Count -gt 0) { $config['LambdaFunctionConfigurations'] = @($newLambdaCfgs) }
if ($topicCfgs.Count -gt 0)     { $config['TopicConfigurations']          = @($topicCfgs) }
if ($queueCfgs.Count -gt 0)     { $config['QueueConfigurations']          = @($queueCfgs) }

if ($WhatIf) {
    Write-Host "`n-WhatIf: this configuration would be written, nothing was changed:" -ForegroundColor Yellow
    Write-Host ($config | ConvertTo-Json -Depth 20)
    return
}

Invoke-Aws @('s3api', 'put-bucket-notification-configuration', '--bucket', $ContentBucket,
    '--notification-configuration', (Save-JsonArg ([pscustomobject]$config) 'notification.json' $workDir),
    '--profile', $AwsProfile, '--region', $Region) | Out-Null

if ($Remove) {
    Write-Ok 'trigger removed; any other notifications on the bucket were preserved'
    Write-Host "`nEdits will no longer re-render static pages. Re-run without -Remove to restore." -ForegroundColor White
    return
}
Write-Ok "s3://$ContentBucket ObjectCreated -> $fnName"

# ------------------------------------------------------------------------ verify
Write-Step 4 'Verify'
$after = Invoke-Aws @('s3api', 'get-bucket-notification-configuration', '--bucket', $ContentBucket,
    '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
$afterCfgs = @()
$raw = Get-Prop $after 'LambdaFunctionConfigurations'
if ($null -ne $raw) { $afterCfgs = @($raw | Where-Object { $null -ne $_ }) }
$missing = @()
foreach ($e in $OurEntries) {
    $found = @($afterCfgs | Where-Object { (Get-Prop $_ 'Id') -eq $e.Id })
    if ($found) { Write-Ok "$($e.Id) is in place" }
    else        { $missing += $e.Id }
}
if ($missing.Count -gt 0) {
    throw "Written but not present on read-back: $($missing -join ', ')"
}

Write-Host "`nDone. Editing an article in the admin now re-renders its static page." -ForegroundColor Green
Write-Host 'Changing the menu, header, site-meta or theme re-renders the WHOLE site,' -ForegroundColor Green
Write-Host 'because all four are baked into every page at render time.' -ForegroundColor Green
Write-Host ''
Write-Host '  header.html needs no notification of its own -- it already arrives via the' -ForegroundColor DarkGray
Write-Host "  article entry's '.html' suffix, and the Lambda's parser routes it." -ForegroundColor DarkGray
Write-Host ''
Write-Host '  Only sites listed in STATIC_PUBLISH_SITES are re-rendered -- the trigger fires for' -ForegroundColor DarkGray
Write-Host '  every site, and the Lambda skips any that has not been cut over.' -ForegroundColor DarkGray
Write-Host ''
Write-Host '  Watch it work:' -ForegroundColor White
Write-Host "    aws logs tail /aws/lambda/$fnName --follow --profile $AwsProfile --region $Region"
