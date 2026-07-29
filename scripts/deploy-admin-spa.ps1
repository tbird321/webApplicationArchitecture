<#
.SYNOPSIS
    Build the React admin SPA for a site and deploy it to that site's admin bucket.

.DESCRIPTION
    The release-time counterpart to deploy-admin-subdomain.ps1 (which is run once
    per site). Run this every time the admin app changes.

    Steps:
      1. Stage the site's assets from configs\ into public\ -- config.json,
         BaseStyles.css, favicon.ico, header.html, initialRender.html,
         headerImage.jpg. The app fetches /config.json at RUNTIME
         (src\hooks\configuration\useConfig.js), so the config has to be a real
         file at the web root, not a build-time constant. public\config.json is
         gitignored precisely because it is per-site scratch.
      2. npm run build (create-react-app copies public\ into build\).
      3. Upload in two passes, because the two kinds of file need opposite caching:
           - /static/* and other hashed assets  -> immutable, one year
           - *.html (index.html above all)      -> no-cache
         Getting this backwards is the classic SPA deploy bug: index.html gets
         cached at the edge, so users keep loading an old shell that references
         JS bundles which no longer exist.
      4. Upload infra\admin\robots.txt.
      5. Invalidate the CloudFront distribution.

.PARAMETER Site
    Site key from sites.json.

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom.

.PARAMETER Region
    Bucket region. Default us-west-2.

.PARAMETER SkipBuild
    Upload whatever is already in react\baseProject\build\ without rebuilding.

.EXAMPLE
    ./scripts/deploy-admin-spa.ps1 -Site ldsapologetics
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Site,

    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region     = 'us-west-2',
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"

$siteInfo  = Resolve-Site -Site $Site
$domain    = $siteInfo.domain
$adminHost = "admin.$domain"
$bucket    = Get-AdminBucketName -SiteKey $siteInfo.key

$repoRoot   = Split-Path $PSScriptRoot -Parent
$appDir     = Join-Path $repoRoot 'react\baseProject'
$buildDir   = Join-Path $appDir 'build'
$configsDir = Join-Path $appDir 'configs'
$publicDir  = Join-Path $appDir 'public'
$robotsPath = Join-Path $repoRoot 'infra\admin\robots.txt'

Write-Host 'Admin SPA deploy' -ForegroundColor White
Write-Host "  site   : $($siteInfo.key)"
Write-Host "  target : https://$adminHost/  (s3://$bucket)"

Test-AwsAuth -AwsProfile $AwsProfile -Region $Region | Out-Null

# The distribution must already exist -- this script does not create infrastructure.
$dist = Get-DistributionByAlias -Alias $adminHost -AwsProfile $AwsProfile
if (-not $dist) {
    throw "No CloudFront distribution has the alias $adminHost. Run: ./scripts/deploy-admin-subdomain.ps1 -Site $($siteInfo.key)"
}
Write-Host "  distro : $($dist.Id)"

# ------------------------------------------------------------------------ build
if ($SkipBuild) {
    if (-not (Test-Path $buildDir)) { throw "-SkipBuild was passed but $buildDir does not exist." }
    Write-Step 1 'Build (skipped)'
    Write-Note "using existing $buildDir"
} else {
    Write-Step 1 "Stage site assets + build ($($siteInfo.key))"

    $prefix = $siteInfo.configPrefix
    if (-not $prefix) {
        $available = (Get-ChildItem $configsDir -Filter '*-Prod.json' | ForEach-Object { $_.Name -replace '-Prod\.json$', '' }) -join ', '
        throw "Site '$($siteInfo.key)' has no configPrefix in sites.json, so there is no config set to build from. Config sets present: $available"
    }

    # Source name in configs\  ->  destination name in public\
    # config.json is deliberately NOT here: it is written separately below, with the
    # analytics tag blanked. Copying it here as well would overwrite that.
    $assets = [ordered]@{
        "$prefix-BaseStyles.css"     = 'BaseStyles.css'
        "$prefix-favicon.ico"        = 'favicon.ico'
        "$prefix-header.html"        = 'header.html'
        "$prefix-initialRender.html" = 'initialRender.html'
        "$prefix-headerImage.jpg"    = 'headerImage.jpg'
    }

    $configSrc = Join-Path $configsDir "$prefix-Prod.json"
    if (-not (Test-Path $configSrc)) { throw "Required config not found: $configSrc" }

    # The SPA initialises react-ga4 from Site.analyticsTag and sends a page_view on every
    # route change (src/sitecontent/SiteContainer.jsx). On the ADMIN host that would report
    # your own CMS sessions into the public site's GA property -- inflating users, wrecking
    # engagement and bounce metrics, and mixing editing traffic in with real readers.
    #
    # The public site's analytics now come from the static pages' own gtag snippet, so the
    # admin build has no reason to send anything. Blank the tag for this build only; the
    # committed config under configs\ is untouched.
    $adminConfig = Get-Content $configSrc -Raw | ConvertFrom-Json
    $originalTag = $adminConfig.Site.analyticsTag
    $adminConfig.Site.analyticsTag = ''
    $utf8NoBomCfg = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText((Join-Path $publicDir 'config.json'),
        ($adminConfig | ConvertTo-Json -Depth 20), $utf8NoBomCfg)
    if ($originalTag) { Write-Ok "analytics disabled for the admin build (site tag $originalTag left for the public pages)" }

    foreach ($src in $assets.Keys) {
        $srcPath = Join-Path $configsDir $src
        if (Test-Path $srcPath) {
            Copy-Item $srcPath (Join-Path $publicDir $assets[$src]) -Force
            Write-Ok "public\$($assets[$src]) <- $src"
        } else {
            # Not every site has every asset (cesletter has no headerImage).
            Write-Note "no $src -- leaving public\$($assets[$src]) as is"
        }
    }

    # create-react-app substitutes %REACT_APP_*% placeholders in public/index.html at
    # BUILD time from the environment. public/index.html has %REACT_APP_SITE_TITLE% and
    # there is no .env file, so without this the browser tab literally reads
    # "%REACT_APP_SITE_TITLE%". Title comes from sites.json, tagged Admin so the tab is
    # distinguishable from the public site.
    $siteTitle = if ($siteInfo.title) { $siteInfo.title } else { $siteInfo.domain }
    $env:REACT_APP_SITE_TITLE = "$siteTitle - Admin"
    Write-Ok "REACT_APP_SITE_TITLE = $($env:REACT_APP_SITE_TITLE)"

    Push-Location $appDir
    try {
        # Streaming, not buffered: this takes 1-3 minutes and silence is
        # indistinguishable from a hang. Invoke-NativeStreaming also keeps WinPS 5.1
        # from promoting npm's stderr progress output to a terminating error under
        # $ErrorActionPreference='Stop'. See lib\SiteInfra.ps1.
        Write-Note 'running npm run build (1-3 min)...'
        $exit = Invoke-NativeStreaming -Exe 'npm' -Arguments @('run', 'build')
        if ($exit -ne 0) { throw "npm run build failed (exit $exit)" }
    } finally { Pop-Location }
    Write-Ok 'build complete'
}

# ----------------------------------------------------------------------- upload
# Anything the app fetches by a FIXED name at runtime must never be cached hard:
# config.json and sitemenu.json are re-read on every load and are not
# content-hashed, so an immutable cache-control on them would pin the site to a
# stale config until the next invalidation.
$mutable = @('*.html', 'config.json', 'sitemenu.json', 'robots.txt')

Write-Step 2 'Upload (content-hashed assets: immutable)'
$immutableArgs = @('s3', 'sync', $buildDir, "s3://$bucket/", '--delete',
    '--profile', $AwsProfile, '--region', $Region,
    '--cache-control', 'public,max-age=31536000,immutable')
foreach ($m in $mutable) { $immutableArgs += @('--exclude', $m) }
Invoke-Aws $immutableArgs | Out-Null
Write-Ok 'assets uploaded'

Write-Step 3 'Upload (HTML: no-cache)'
Invoke-Aws @('s3', 'sync', $buildDir, "s3://$bucket/",
    '--profile', $AwsProfile, '--region', $Region,
    '--cache-control', 'no-cache',
    '--content-type', 'text/html; charset=utf-8',
    '--exclude', '*', '--include', '*.html') | Out-Null
Write-Ok 'html uploaded'

Write-Step 4 'Upload (runtime JSON: no-cache)'
Invoke-Aws @('s3', 'sync', $buildDir, "s3://$bucket/",
    '--profile', $AwsProfile, '--region', $Region,
    '--cache-control', 'no-cache',
    '--content-type', 'application/json',
    '--exclude', '*', '--include', 'config.json', '--include', 'sitemenu.json') | Out-Null
Write-Ok 'config.json / sitemenu.json uploaded'

Write-Step 5 'robots.txt (Disallow: /)'
if (Test-Path $robotsPath) {
    Invoke-Aws @('s3', 'cp', $robotsPath, "s3://$bucket/robots.txt",
        '--profile', $AwsProfile, '--region', $Region,
        '--cache-control', 'no-cache', '--content-type', 'text/plain') | Out-Null
    Write-Ok 'uploaded'
} else {
    Write-Warn "$robotsPath not found -- the admin site will have no robots.txt (the X-Robots-Tag header still applies)."
}

# ------------------------------------------------------------------- invalidate
Write-Step 6 'CloudFront invalidation'
$inv = Invoke-Aws @('cloudfront', 'create-invalidation', '--distribution-id', $dist.Id,
    '--paths', '/*', '--profile', $AwsProfile, '--output', 'json')
Write-Ok "invalidation $($inv.Invalidation.Id) created"

Write-Host "`nDeployed. https://$adminHost/" -ForegroundColor White
Write-Host '  The invalidation takes a minute or two to complete:'
Write-Host "    aws cloudfront wait invalidation-completed --distribution-id $($dist.Id) --id $($inv.Invalidation.Id) --profile $AwsProfile"
