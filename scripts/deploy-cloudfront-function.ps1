<#
.SYNOPSIS
    Publish infra\cloudfront\public-clean-urls.js as a CloudFront Function and
    attach it to a public site distribution's viewer-request event.

.DESCRIPTION
    Step 4 of STATIC-PRERENDER-ROLLOUT.md. The function does two things (see the
    comments in the .js file):

      1. 301-redirects legacy SPA deep links -- /?page=Temple-And-Masonry becomes
         /temple-and-masonry/ -- so old bookmarks, shared links, and everything
         already in Google's index keep working AND pass their link equity to the
         new clean URL. A 301 (not a 302) is what transfers ranking.

      2. Rewrites clean paths to the pre-rendered objects: /slug -> /slug/index.html.

    The function body carries no site-specific values, so ONE function is created
    and attached to every public distribution. Re-running updates the code in place
    and re-publishes.

    ORDERING WARNING: attach this only AFTER the static pages have been backfilled
    (publish-static-pages.ps1 -Upload). Attach it to an empty bucket and every
    clean URL rewrites to an object that does not exist yet.

    Never attach it to an ADMIN distribution -- that one needs SPA fallback
    routing, which this function's rewrites would break.

.PARAMETER Site
    Site key from sites.json. The public distribution is found by its www.{domain}
    alias (falling back to the apex).

.PARAMETER Domain
    For a site not yet in sites.json.

.PARAMETER DistributionId
    Skip alias lookup and target this distribution directly.

.PARAMETER PublishOnly
    Create/update and publish the function, but do not attach it to any
    distribution. Useful for staging the code ahead of the cutover.

.PARAMETER Detach
    Remove the function from the distribution's viewer-request event. The rollback
    switch -- the function itself is left published and reusable.

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom.

.EXAMPLE
    ./scripts/deploy-cloudfront-function.ps1 -Site ldsapologetics

.EXAMPLE
    # Stage the code without changing live routing
    ./scripts/deploy-cloudfront-function.ps1 -Site ldsapologetics -PublishOnly

.EXAMPLE
    # Roll back
    ./scripts/deploy-cloudfront-function.ps1 -Site ldsapologetics -Detach
#>

[CmdletBinding(DefaultParameterSetName = 'ByKey')]
param(
    [Parameter(ParameterSetName = 'ByKey', Mandatory = $true, Position = 0)]
    [string]$Site,

    [Parameter(ParameterSetName = 'ByDomain', Mandatory = $true)]
    [string]$Domain,

    [Parameter(ParameterSetName = 'ById', Mandatory = $true)]
    [string]$DistributionId,

    [string]$FunctionName = 'public-clean-urls',
    [string]$AwsProfile   = 'tbirdcontractinggmailcom',
    [string]$Region       = 'us-west-2',

    [switch]$PublishOnly,
    [switch]$Detach
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"

$repoRoot     = Split-Path $PSScriptRoot -Parent
$functionFile = Join-Path $repoRoot 'infra\cloudfront\public-clean-urls.js'
$workDir      = Join-Path ([System.IO.Path]::GetTempPath()) 'cloudfront-function'

if (-not (Test-Path $functionFile)) { throw "Function source not found: $functionFile" }

Write-Host 'CloudFront Function deploy' -ForegroundColor White
Write-Host "  function : $FunctionName"
Write-Host "  source   : infra\cloudfront\public-clean-urls.js"

Test-AwsAuth -AwsProfile $AwsProfile -Region $Region | Out-Null

# ------------------------------------------------------- resolve the distribution
$targetId = $null
$targetLabel = ''
if ($DistributionId) {
    $targetId = $DistributionId
    $targetLabel = $DistributionId
} elseif (-not $PublishOnly) {
    $siteInfo = if ($Site) { Resolve-Site -Site $Site } else { Resolve-Site -Domain $Domain }
    $publicHost = "www.$($siteInfo.domain)"

    $dist = Get-DistributionByAlias -Alias $publicHost -AwsProfile $AwsProfile
    if (-not $dist) {
        $dist = Get-DistributionByAlias -Alias $siteInfo.domain -AwsProfile $AwsProfile
        if ($dist) { $publicHost = $siteInfo.domain }
    }
    if (-not $dist) {
        throw "No CloudFront distribution found with alias www.$($siteInfo.domain) or $($siteInfo.domain). Pass -DistributionId explicitly."
    }

    # Guard: refuse to touch an admin distribution.
    if ($publicHost -like 'admin.*') {
        throw "Refusing to attach the clean-URL function to an admin distribution ($publicHost) -- it would break SPA routing."
    }

    $targetId = $dist.Id
    $targetLabel = "$publicHost ($($dist.Id))"
    Write-Host "  target   : $targetLabel"
}

# ------------------------------------------------- 1. create or update the function
Write-Step 1 'Function code'

$existingEtag = $null
try {
    $desc = Invoke-Aws @('cloudfront', 'describe-function', '--name', $FunctionName,
        '--stage', 'DEVELOPMENT', '--profile', $AwsProfile, '--output', 'json')
    $existingEtag = $desc.ETag
} catch {
    # NoSuchFunctionExists -- first deploy.
    $existingEtag = $null
}

# --function-code needs fileb:// (it is sent as a blob, not text).
$codeArg = "fileb://$($functionFile -replace '\\','/')"
$configArg = "Comment=Clean URLs + legacy ?page= 301 redirects,Runtime=cloudfront-js-2.0"

if ($existingEtag) {
    $updated = Invoke-Aws @('cloudfront', 'update-function', '--name', $FunctionName,
        '--if-match', $existingEtag, '--function-config', $configArg,
        '--function-code', $codeArg, '--profile', $AwsProfile, '--output', 'json')
    $etag = $updated.ETag
    Write-Ok "updated $FunctionName (DEVELOPMENT)"
} else {
    $created = Invoke-Aws @('cloudfront', 'create-function', '--name', $FunctionName,
        '--function-config', $configArg, '--function-code', $codeArg,
        '--profile', $AwsProfile, '--output', 'json')
    $etag = $created.ETag
    Write-Ok "created $FunctionName (DEVELOPMENT)"
}

# ------------------------------------------------------------- 2. publish to LIVE
Write-Step 2 'Publish DEVELOPMENT -> LIVE'
$published = Invoke-Aws @('cloudfront', 'publish-function', '--name', $FunctionName,
    '--if-match', $etag, '--profile', $AwsProfile, '--output', 'json')
$functionArn = $published.FunctionSummary.FunctionMetadata.FunctionARN
Write-Ok "published: $functionArn"

if ($PublishOnly) {
    Write-Host "`nFunction is published but attached to nothing (-PublishOnly)." -ForegroundColor White
    Write-Host "  Attach it when the static pages are live:"
    Write-Host "    ./scripts/deploy-cloudfront-function.ps1 -Site <key>"
    return
}

# --------------------------------------------- 3. associate with the distribution
Write-Step 3 $(if ($Detach) { "Detach from $targetLabel" } else { "Attach to $targetLabel (viewer-request)" })

$current = Invoke-Aws @('cloudfront', 'get-distribution-config', '--id', $targetId,
    '--profile', $AwsProfile, '--output', 'json')
$distEtag = $current.ETag
$config   = $current.DistributionConfig

# If the desired association is already in place, skip the update entirely.
#
# This matters because the function is attached by ARN, not by version: republishing
# the function code takes effect immediately on every distribution it is attached to.
# So re-running this script to ship a code fix does NOT need to touch the distribution
# at all -- and attempting to would risk a PreconditionFailed on a stale ETag for no
# benefit whatsoever.
$alreadyAttached = $false
$existingAssoc = Get-Prop $config 'DefaultCacheBehavior.FunctionAssociations'
if ($existingAssoc -and $existingAssoc.Quantity -gt 0) {
    foreach ($a in @(Get-Prop $existingAssoc 'Items')) {
        if ($a -and $a.EventType -eq 'viewer-request' -and $a.FunctionARN -eq $functionArn) {
            $alreadyAttached = $true
        }
    }
}

if ($alreadyAttached -and -not $Detach) {
    Write-Ok "already attached to $targetLabel -- the republished code is live, no distribution update needed"
    Write-Host "`nFunction code updated and published. No CloudFront redeploy required." -ForegroundColor White
    if ($Site -or $Domain) {
        $d = (Resolve-Site -Site $Site -Domain $Domain).domain
        Write-Host "`nVerify:" -ForegroundColor White
        Write-Host "  curl.exe -s -o NUL -D - ""https://www.$d/?page=Home""   # expect 301 -> /"
    }
    return
}

# Preserve any association on other event types; replace only viewer-request.
$kept = @()
if ($config.DefaultCacheBehavior.PSObject.Properties['FunctionAssociations'] -and
    $config.DefaultCacheBehavior.FunctionAssociations.Quantity -gt 0) {
    foreach ($a in $config.DefaultCacheBehavior.FunctionAssociations.Items) {
        if ($a.EventType -ne 'viewer-request') { $kept += $a }
    }
}

if (-not $Detach) {
    $kept += [pscustomobject]@{ EventType = 'viewer-request'; FunctionARN = $functionArn }
}

$assoc = if ($kept.Count -gt 0) {
    [pscustomobject]@{ Quantity = $kept.Count; Items = @($kept) }
} else {
    [pscustomobject]@{ Quantity = 0 }
}

if ($config.DefaultCacheBehavior.PSObject.Properties['FunctionAssociations']) {
    $config.DefaultCacheBehavior.FunctionAssociations = $assoc
} else {
    $config.DefaultCacheBehavior | Add-Member -NotePropertyName 'FunctionAssociations' -NotePropertyValue $assoc
}

Invoke-Aws @('cloudfront', 'update-distribution', '--id', $targetId,
    '--if-match', $distEtag,
    '--distribution-config', (Save-JsonArg $config 'distribution-config.json' $workDir),
    '--profile', $AwsProfile, '--output', 'json') | Out-Null

if ($Detach) {
    Write-Ok "detached from $targetLabel"
} else {
    Write-Ok "attached to $targetLabel"
}

# --------------------------------------------------------------------------- done
Write-Host "`nCloudFront is redeploying (~5-10 min):" -ForegroundColor White
Write-Host "  aws cloudfront wait distribution-deployed --id $targetId --profile $AwsProfile"

if (-not $Detach) {
    Write-Host "`nThen verify the 301 and the rewrite:" -ForegroundColor White
    if ($Site -or $Domain) {
        $d = (Resolve-Site -Site $Site -Domain $Domain).domain
        # curl.exe, not curl: in PowerShell 5.1 `curl` is an alias for Invoke-WebRequest,
        # which has no -sI. And ?page=Home rather than a named page, because the old example
        # was an ldsapologetics page -- on any other site it 301s to a genuine 404 and looks
        # like the cutover broke something.
        Write-Host "  curl.exe -sI ""https://www.$d/?page=Home""   # expect 301 -> /"
        Write-Host "  curl.exe -sI ""https://www.$d/""             # expect 200"
    }
    Write-Host ''
    Write-Host '  Roll back with -Detach if anything looks wrong.' -ForegroundColor DarkGray
}
