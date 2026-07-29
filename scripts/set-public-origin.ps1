<#
.SYNOPSIS
    Point a public CloudFront distribution at its bucket's S3 WEBSITE endpoint.

.DESCRIPTION
    The static migration depends on S3 website hosting resolving /slug/ to
    /slug/index.html by itself. That is what makes the cutover safe: the new clean URLs
    serve correctly BEFORE any routing change, so `-Phase verify` can prove the new world
    is healthy while the old one is still fully intact.

    That resolution only happens at the S3 WEBSITE endpoint:

        www.example.com.s3-website-us-west-2.amazonaws.com    index resolution, real 404s
        www.example.com.s3.amazonaws.com                      REST -- /slug/ is just a
                                                              missing key, and S3 answers
                                                              403 (it hides 404 from
                                                              callers without ListBucket)

    Distributions created against the REST endpoint therefore 403 on every clean URL, and
    the phased cutover cannot verify anything until routing is already live. This repoints
    the origin so those sites behave like the rest.

    MINIMAL AND IDEMPOTENT. Only the first origin's DomainName and origin-type block are
    touched. The origin Id is deliberately left alone so DefaultCacheBehavior.TargetOriginId
    and any additional cache behaviours keep matching without being rewritten.

    PREREQUISITE: the bucket must already have website hosting enabled and be readable
    anonymously -- the website endpoint is HTTP-only and unsigned, so CloudFront fetches it
    as an anonymous client. This is checked before anything is changed.

.PARAMETER Site
    Site key from sites.json.

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom

.PARAMETER Wait
    Block until the distribution finishes deploying (typically 5-10 minutes).

.PARAMETER WhatIf
    Report what would change without changing it.

.EXAMPLE
    ./scripts/set-public-origin.ps1 -Site cesletter -WhatIf

.EXAMPLE
    ./scripts/set-public-origin.ps1 -Site cesletter -Wait
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Site,
    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region     = 'us-west-2',
    [switch]$Wait,
    [switch]$WhatIf
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"

$siteInfo   = Resolve-Site -Site $Site
$domain     = $siteInfo.domain
$pubBucket  = "www.$domain"
$websiteHost = "$pubBucket.s3-website-$Region.amazonaws.com"
$workDir    = Join-Path ([System.IO.Path]::GetTempPath()) "origin-$($siteInfo.key)"

Write-Host "`nPublic origin: $($siteInfo.key)  ($domain)" -ForegroundColor White
Write-Host "  target origin : $websiteHost"
Write-Host "  profile       : $AwsProfile"

Test-AwsAuth -AwsProfile $AwsProfile -Region $Region | Out-Null

# ------------------------------------------------------- 1. bucket must be servable
Write-Step 1 'Confirm the bucket can serve as a website origin'

$web = Invoke-Aws @('s3api', 'get-bucket-website', '--bucket', $pubBucket,
    '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
$indexDoc = Get-Prop $web 'IndexDocument.Suffix'
if (-not $indexDoc) {
    throw @"
s3://$pubBucket has no website configuration, so the website endpoint cannot serve it.
Enable it first:

    aws s3 website s3://$pubBucket --index-document index.html --error-document index.html --profile $AwsProfile
"@
}
Write-Ok "website hosting enabled (index=$indexDoc)"

# The website endpoint is anonymous HTTP. If the bucket policy does not allow public reads
# the swap would take the whole site down, so this is proven against the live endpoint
# rather than inferred from the policy document.
try {
    $probe = Invoke-WebRequest -Uri "http://$websiteHost/" -UseBasicParsing -TimeoutSec 20
    Write-Ok "website endpoint responds (HTTP $($probe.StatusCode))"
} catch {
    throw @"
http://$websiteHost/ is not serving ($($_.Exception.Message)).

The S3 website endpoint is anonymous and HTTP-only, so the bucket must allow public reads.
Repointing CloudFront at an endpoint that does not serve would take the site down. Fix the
bucket policy first, confirm the URL above loads in a browser, then re-run.
"@
}

# ------------------------------------------------------------ 2. find the distribution
Write-Step 2 'Locate the public distribution'

$dist = Get-DistributionByAlias -Alias $pubBucket -AwsProfile $AwsProfile
if (-not $dist) { $dist = Get-DistributionByAlias -Alias $domain -AwsProfile $AwsProfile }
if (-not $dist) { throw "No CloudFront distribution has alias '$pubBucket' or '$domain'." }
Write-Ok "$($dist.Id)  ($($dist.DomainName))"

$cfg = Invoke-Aws @('cloudfront', 'get-distribution-config', '--id', $dist.Id,
    '--profile', $AwsProfile, '--output', 'json')
$etag = Get-Prop $cfg 'ETag'
$dc   = Get-Prop $cfg 'DistributionConfig'
if (-not $etag -or -not $dc) { throw "Could not read the distribution config for $($dist.Id)." }

$origins = @(Get-Prop $dc 'Origins.Items')
if ($origins.Count -eq 0) { throw "Distribution $($dist.Id) reports no origins." }
$origin = $origins[0]
$currentDomain = Get-Prop $origin 'DomainName'
Write-Note "current origin: $currentDomain"

# ------------------------------------------------------------------- 3. already done?
Write-Step 3 'Repoint the origin at the website endpoint'

if ($currentDomain -eq $websiteHost) {
    Write-Ok 'already pointing at the website endpoint -- nothing to do'
    return
}
if ($currentDomain -like '*s3-website*') {
    Write-Ok "already a website endpoint ($currentDomain) -- leaving it alone"
    return
}

# Swap the domain and the origin-type block. The Id is left untouched on purpose: it is
# referenced by DefaultCacheBehavior.TargetOriginId and by every additional cache
# behaviour, and renaming it would mean rewriting all of them in the same call.
$origin.DomainName = $websiteHost
if ($origin.PSObject.Properties['S3OriginConfig']) {
    $origin.PSObject.Properties.Remove('S3OriginConfig')
}

# Matches the working sites exactly. http-only is not a choice -- S3 website endpoints do
# not speak HTTPS. Viewers still reach CloudFront over HTTPS; only the CloudFront-to-S3 hop
# is plaintext, inside AWS, to a bucket that is public anyway.
$customOrigin = [pscustomobject]@{
    HTTPPort               = 80
    HTTPSPort              = 443
    OriginProtocolPolicy   = 'http-only'
    OriginSslProtocols     = [pscustomobject]@{
        Quantity = 4
        Items    = @('SSLv3', 'TLSv1', 'TLSv1.1', 'TLSv1.2')
    }
    OriginReadTimeout      = 30
    OriginKeepaliveTimeout = 5
}
if ($origin.PSObject.Properties['CustomOriginConfig']) {
    $origin.CustomOriginConfig = $customOrigin
} else {
    $origin | Add-Member -NotePropertyName 'CustomOriginConfig' -NotePropertyValue $customOrigin
}

# OriginAccessControlId is meaningless for a custom origin and AWS rejects a populated one.
if ($origin.PSObject.Properties['OriginAccessControlId']) { $origin.OriginAccessControlId = '' }

Write-Note "new origin    : $websiteHost  (custom, http-only)"

if ($WhatIf) {
    Write-Host "`n-WhatIf: this origin would be written, nothing was changed:" -ForegroundColor Yellow
    Write-Host ($origin | ConvertTo-Json -Depth 10)
    return
}

Invoke-Aws @('cloudfront', 'update-distribution', '--id', $dist.Id,
    '--if-match', $etag,
    '--distribution-config', (Save-JsonArg $dc 'distribution-config.json' $workDir),
    '--profile', $AwsProfile, '--output', 'json') | Out-Null

Write-Ok "origin updated -- CloudFront is deploying the change"

# --------------------------------------------------------------------------- 4. verify
Write-Step 4 'Verify'

if ($Wait) {
    Write-Host '  waiting for the distribution to finish deploying (5-10 min)...' -ForegroundColor DarkGray
    Invoke-Aws @('cloudfront', 'wait', 'distribution-deployed', '--id', $dist.Id,
        '--profile', $AwsProfile) | Out-Null
    Write-Ok 'deployed'
} else {
    Write-Note 'not waiting; the change takes 5-10 minutes to reach every edge'
}

Write-Host ''
Write-Host 'Next:' -ForegroundColor White
Write-Host "  1. wait for the deploy to finish (aws cloudfront wait distribution-deployed --id $($dist.Id) --profile $AwsProfile)"
Write-Host "  2. ./scripts/check-site-health.ps1 -Site $($siteInfo.key)"

