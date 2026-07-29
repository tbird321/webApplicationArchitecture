<#
.SYNOPSIS
    Stand up admin.{domain} for a site: private S3 bucket, ACM certificate,
    CloudFront distribution (OAC + SPA routing + noindex), and the GoDaddy CNAME.

.DESCRIPTION
    Step 5 of STATIC-PRERENDER-ROLLOUT.md, entirely via the AWS CLI. The public
    site moves to pre-rendered static HTML at the root; the React SPA moves here.

    Idempotent -- every resource is looked up before it is created, so re-running
    after a failure (or after adding a DNS record by hand) picks up where it left
    off. Nothing is destroyed.

    What it creates, in order:
      1. Private S3 bucket, all public access blocked, NO website hosting.
      2. ACM certificate in us-east-1 (CloudFront only accepts certs from there),
         DNS-validated with a CNAME at GoDaddy.
      3. CloudFront Origin Access Control.
      4. Response headers policy adding X-Robots-Tag: noindex, nofollow.
      5. CloudFront distribution -- HTTPS only, index.html root, and 403/404
         rewritten to /index.html with a 200 so SPA deep links work.
      6. Bucket policy scoped to that distribution's ARN (must come after 5).
      7. CNAME admin -> {dist}.cloudfront.net at GoDaddy.

    DNS: these domains are resolved by GoDaddy, not Route 53. Set GODADDY_PAT (a
    Personal Access Token with the "domains.dns:update" scope) to have records
    created automatically; otherwise the script prints each record and waits while
    you add it in the GoDaddy DNS panel. A legacy GODADDY_API_KEY/_SECRET pair is
    still accepted. See lib\GoDaddyDns.ps1.

    It does NOT build or upload the SPA -- that is deploy-admin-spa.ps1, kept
    separate because you run it on every release, not once per site.

.PARAMETER Site
    Site key from sites.json (ldsapologetics, ldsdoctrines, ...).

.PARAMETER Domain
    For a brand-new site not yet in sites.json. Mutually exclusive with -Site.

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom (the upload profile).

.PARAMETER Region
    Region for the S3 bucket. Default us-west-2. ACM and CloudFront are always
    driven through us-east-1 regardless.

.PARAMETER PriceClass
    CloudFront price class. Default PriceClass_100 (US/EU) -- this is a private
    admin console, so there is no reason to pay for worldwide edge coverage.

.PARAMETER SkipDns
    Create the AWS resources but do not touch DNS or wait for it. Useful when you
    want to review the distribution before pointing a hostname at it.

.EXAMPLE
    ./scripts/deploy-admin-subdomain.ps1 -Site ldsapologetics

.EXAMPLE
    # A new site, before it has been added to sites.json
    ./scripts/deploy-admin-subdomain.ps1 -Domain newsite.com
#>

[CmdletBinding(DefaultParameterSetName = 'ByKey')]
param(
    [Parameter(ParameterSetName = 'ByKey', Mandatory = $true, Position = 0)]
    [string]$Site,

    [Parameter(ParameterSetName = 'ByDomain', Mandatory = $true)]
    [string]$Domain,

    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region     = 'us-west-2',

    [ValidateSet('PriceClass_100', 'PriceClass_200', 'PriceClass_All')]
    [string]$PriceClass = 'PriceClass_100',

    [switch]$SkipDns
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"
. "$PSScriptRoot\lib\GoDaddyDns.ps1"

# ------------------------------------------------------------------ resolve site
$siteInfo  = if ($Site) { Resolve-Site -Site $Site } else { Resolve-Site -Domain $Domain }
$domain    = $siteInfo.domain
$adminHost = "admin.$domain"
$bucket    = Get-AdminBucketName -SiteKey $siteInfo.key
$workDir   = Join-Path ([System.IO.Path]::GetTempPath()) "admin-subdomain-$($siteInfo.key)"

Write-Host 'Admin subdomain standup' -ForegroundColor White
Write-Host "  site    : $($siteInfo.key)$(if ($siteInfo.id) { "  (WEBSITE_ID $($siteInfo.id))" })"
Write-Host "  host    : https://$adminHost/"
Write-Host "  bucket  : s3://$bucket   ($Region, private)"
$dnsMode = switch (Get-GoDaddyAuthKind) {
    'pat'     { 'API via Personal Access Token' }
    'sso-key' { 'API via legacy key/secret (retired after 2026 -- move to a PAT)' }
    default   { 'manual -- set GODADDY_PAT to automate' }
}
Write-Host "  dns     : GoDaddy ($dnsMode)"
Write-Host "  profile : $AwsProfile"

$identity = Test-AwsAuth -AwsProfile $AwsProfile -Region $Region
Write-Host "  account : $($identity.Account)"

# ---------------------------------------------------------------------- 1. bucket
Write-Step 1 'S3 bucket (private)'
New-PrivateBucket -Bucket $bucket -AwsProfile $AwsProfile -Region $Region

# ----------------------------------------------------------------- 2. certificate
Write-Step 2 "ACM certificate for $adminHost (us-east-1)"
$cert = Get-OrNewCertificate -HostName $adminHost -Domain $domain -AwsProfile $AwsProfile

if ($cert.IsIssued) {
    Write-Skip "certificate $($cert.Arn)"
} elseif ($SkipDns) {
    Write-Warn 'Certificate needs DNS validation, but -SkipDns was passed. Add this record, then re-run:'
    Show-ManualDnsRecord -Domain $domain `
        -Name (ConvertTo-RelativeName -FullName $cert.ValidationName -Domain $domain) `
        -Type 'CNAME' -Value $cert.ValidationValue -Purpose 'ACM certificate validation'
    return
} else {
    $ok = Set-DnsRecord -Domain $domain -FullName $cert.ValidationName -Type 'CNAME' `
        -Value $cert.ValidationValue -Purpose 'ACM certificate validation'
    if (-not $ok) {
        throw "The ACM validation record for $adminHost never appeared in public DNS. Verify it at GoDaddy and re-run -- the script will resume from here."
    }
    Wait-CertificateIssued -CertificateArn $cert.Arn -AwsProfile $AwsProfile
}
$certArn = $cert.Arn

# ------------------------------------------------------------------------- 3. OAC
Write-Step 3 'CloudFront Origin Access Control'
$oacId = Get-OrNewOriginAccessControl -Name "$bucket-oac" -Description "OAC for $adminHost" `
    -AwsProfile $AwsProfile -WorkDir $workDir

# --------------------------------------------------------- 4. noindex headers policy
Write-Step 4 'Response headers policy (noindex)'
$rhpId = Get-OrNewNoIndexHeadersPolicy -AwsProfile $AwsProfile -WorkDir $workDir

# ---------------------------------------------------------------- 5. distribution
Write-Step 5 'CloudFront distribution'
$dist = Get-OrNewSpaDistribution -Alias $adminHost -Bucket $bucket -BucketRegion $Region `
    -CertificateArn $certArn -OacId $oacId -ResponseHeadersPolicyId $rhpId `
    -CallerReference "admin-$($siteInfo.key)-v1" `
    -Comment "$adminHost -- CMS admin SPA (Cognito-gated, noindex)" `
    -PriceClass $PriceClass -AwsProfile $AwsProfile -WorkDir $workDir

# --------------------------------------------------------------- 6. bucket policy
Write-Step 6 'Bucket policy (CloudFront service principal)'
Set-CloudFrontOriginBucketPolicy -Bucket $bucket -DistributionArn $dist.Arn `
    -AwsProfile $AwsProfile -Region $Region -WorkDir $workDir

# ------------------------------------------------------------------------- 7. DNS
Write-Step 7 "DNS: $adminHost -> $($dist.DomainName)"
if ($SkipDns) {
    Write-Note '-SkipDns passed. The record you will need:'
    Show-ManualDnsRecord -Domain $domain -Name 'admin' -Type 'CNAME' -Value $dist.DomainName `
        -Purpose 'point the admin subdomain at CloudFront'
} else {
    # A CNAME is correct here because admin.{domain} is a subdomain. Only the apex
    # would need an ALIAS record, which GoDaddy cannot point at CloudFront.
    $ok = Set-DnsRecord -Domain $domain -FullName $adminHost -Type 'CNAME' `
        -Value $dist.DomainName -Purpose 'point the admin subdomain at CloudFront'
    if (-not $ok) { Write-Warn 'DNS has not propagated yet -- it will resolve on its own; the AWS side is done.' }
}

# ------------------------------------------------------------------------- done
Write-Host "`nInfrastructure is in place." -ForegroundColor White
Write-Host "  CloudFront takes ~5-10 minutes to finish deploying:"
Write-Host "    aws cloudfront wait distribution-deployed --id $($dist.Id) --profile $AwsProfile"
Write-Host ''
Write-Host '  Then ship the SPA:' -ForegroundColor White
Write-Host "    ./scripts/deploy-admin-spa.ps1 -Site $($siteInfo.key)"
Write-Host ''
Write-Host "  Distribution : $($dist.Id)  ($($dist.DomainName))"
Write-Host "  Bucket       : s3://$bucket"
Write-Host "  Admin URL    : https://$adminHost/"

if (-not $siteInfo.id) {
    Write-Warn "'$($siteInfo.key)' is not in sites.json -- add it so the other scripts can find it."
}
