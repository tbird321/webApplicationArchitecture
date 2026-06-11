<#
.SYNOPSIS
    Regenerate sitemap.xml for one or all CMS-hosted sites and upload to S3.

.DESCRIPTION
    Queries the CMS API for every page on a given site, filters to those that
    are published (or have a null/legacy status that still serves), builds a
    standards-compliant sitemap.xml, and uploads it to the site's S3 root.

    No Lambda, no new infrastructure. Run whenever you publish a content
    batch and want crawlers to see the new URLs.

.PARAMETER Site
    The site to regenerate. Accepts a site key (ldsapologetics, ldsdoctrines,
    ldsdiscussions, cesletter, ldsfaithincrisis, reflectiverealizations) or
    'all'. Default: all.

.PARAMETER NoUpload
    Build sitemap files into .\dist\sitemaps\ locally without uploading to S3.
    Useful for previewing the output before pushing.

.PARAMETER AwsProfile
    AWS CLI profile to use for the upload. Default: tbirdcontractinggmailcom.

.PARAMETER Bucket
    S3 bucket name. Default: www-websitecontent.

.EXAMPLE
    .\regenerate-sitemaps.ps1
    Regenerates and uploads sitemap.xml for all sites.

.EXAMPLE
    .\regenerate-sitemaps.ps1 -Site ldsapologetics
    Only regenerates and uploads for ldsapologetics.com.

.EXAMPLE
    .\regenerate-sitemaps.ps1 -NoUpload
    Builds all site sitemaps locally for inspection. Nothing touches S3.

.NOTES
    Requires the following env vars (same as the MCP server):
        LAMBDA_API_BASE_URL   - e.g. https://xxxx.execute-api.us-west-2.amazonaws.com/prod
        MCP_API_KEY           - the CMS API key

    Requires the AWS CLI to be installed and the specified profile configured.

    Each site is uploaded to the PUBLIC web hosting bucket named www.{domain}
    (matching the Lambda's regenerate_sitemap endpoint), with files placed at
    the bucket root so they serve directly at https://www.{domain}/sitemap.xml
    and https://www.{domain}/robots.txt. CloudFront invalidation is best-effort.

.PARAMETER NoInvalidate
    Skip the CloudFront invalidation step. Useful if you don't have
    cloudfront:CreateInvalidation permission on this profile.
#>

param(
    [ValidateSet('all', 'ldsapologetics', 'ldsdoctrines', 'ldsdiscussions', 'cesletter', 'ldsfaithincrisis', 'reflectiverealizations')]
    [string]$Site = 'all',
    [switch]$NoUpload,
    [switch]$NoInvalidate,
    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region    = 'us-west-2'
)

# Set region env vars so any AWS CLI subprocess that doesn't take --region still works.
# (The profile 'tbirdcontractinggmailcom' has no default region configured.)
if (-not [string]::IsNullOrEmpty($Region)) {
    $env:AWS_REGION         = $Region
    $env:AWS_DEFAULT_REGION = $Region
}

# ----- Site config -------------------------------------------------------
# Each entry maps a friendly key to the WebsiteId in the DB and the public
# domain. The web hosting bucket is assumed to be named www.{Domain}, with
# files served from the bucket root.
$sites = @(
    @{ Key = 'ldsfaithincrisis';       Id = 1; Domain = 'ldsfaithincrisis.com' }
    @{ Key = 'ldsdoctrines';           Id = 2; Domain = 'ldsdoctrines.com' }
    @{ Key = 'reflectiverealizations'; Id = 4; Domain = 'reflectiverealizations.com' }
    @{ Key = 'ldsapologetics';         Id = 5; Domain = 'ldsapologetics.com' }
    @{ Key = 'ldsdiscussions';         Id = 6; Domain = 'ldsdiscussions.info' }
    @{ Key = 'cesletter';              Id = 8; Domain = 'cesletter.info' }
)

# ----- Validate env ------------------------------------------------------
$apiBase = $env:LAMBDA_API_BASE_URL
$apiKey  = $env:MCP_API_KEY
if (-not $apiBase) { Write-Error 'LAMBDA_API_BASE_URL env var is not set.'; exit 1 }
if (-not $apiKey)  { Write-Error 'MCP_API_KEY env var is not set.';        exit 1 }
$apiBase = $apiBase.TrimEnd('/')

if (-not $NoUpload) {
    $aws = Get-Command aws -ErrorAction SilentlyContinue
    if (-not $aws) {
        Write-Warning 'AWS CLI not found on PATH. Use -NoUpload to skip the upload step, or install/configure the AWS CLI.'
        exit 1
    }
}

# ----- Helpers -----------------------------------------------------------
function Get-AllPages {
    param([Parameter(Mandatory)][int]$WebsiteId)

    $body = @{
        Name        = $null
        Keywords    = @()
        Topics      = @()
        Description = $null
        WebsiteId   = $WebsiteId
    } | ConvertTo-Json -Depth 5

    $headers = @{
        'Content-Type' = 'application/json'
        'X-API-Key'    = $apiKey
    }

    return Invoke-RestMethod -Uri "$apiBase/page/search" -Method Post -Body $body -Headers $headers
}

function Test-IsServedPage {
    param($Page)
    # A page is served when its status is "published" OR null (legacy pages
    # predating status tracking that still resolve on the site).
    if (-not $Page) { return $false }
    if ([string]::IsNullOrWhiteSpace($Page.name)) { return $false }
    if (-not $Page.status) { return $true }
    return ($Page.status -eq 'published')
}

function New-SitemapXml {
    param(
        [Parameter(Mandatory)][string]$Domain,
        [Parameter(Mandatory)][array]$Pages
    )

    $today = (Get-Date).ToString('yyyy-MM-dd')

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<?xml version="1.0" encoding="UTF-8"?>')
    [void]$sb.AppendLine('<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">')

    # Home page (priority 1.0)
    [void]$sb.AppendLine('  <url>')
    [void]$sb.AppendLine("    <loc>https://www.$Domain/</loc>")
    [void]$sb.AppendLine("    <lastmod>$today</lastmod>")
    [void]$sb.AppendLine('    <changefreq>weekly</changefreq>')
    [void]$sb.AppendLine('    <priority>1.0</priority>')
    [void]$sb.AppendLine('  </url>')

    # One <url> per published page
    foreach ($p in $Pages) {
        $slug    = [uri]::EscapeDataString($p.name)
        [void]$sb.AppendLine('  <url>')
        [void]$sb.AppendLine("    <loc>https://www.$Domain/?page=$slug</loc>")
        [void]$sb.AppendLine("    <lastmod>$today</lastmod>")
        [void]$sb.AppendLine('    <changefreq>monthly</changefreq>')
        [void]$sb.AppendLine('    <priority>0.7</priority>')
        [void]$sb.AppendLine('  </url>')
    }

    [void]$sb.AppendLine('</urlset>')
    return $sb.ToString()
}

# ----- Output directory --------------------------------------------------
$outDir = Join-Path $PSScriptRoot '..\dist\sitemaps'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# ----- Select sites ------------------------------------------------------
$selected = if ($Site -eq 'all') { $sites } else { $sites | Where-Object { $_.Key -eq $Site } }
if (-not $selected) {
    Write-Error "Unknown site key: $Site"
    exit 1
}

# ----- Main loop ---------------------------------------------------------
$totals = @{}
foreach ($s in $selected) {
    Write-Host ""
    Write-Host "==> $($s.Domain)  (WebsiteId=$($s.Id))" -ForegroundColor Cyan

    try {
        $all = Get-AllPages -WebsiteId $s.Id
        if (-not $all) { Write-Warning '   API returned no pages.'; continue }

        $published = @($all | Where-Object { Test-IsServedPage $_ })
        Write-Host ("   {0} pages total, {1} served" -f $all.Count, $published.Count)

        # Use a BOM-less UTF-8 encoder. Windows PowerShell 5.1's Set-Content -Encoding utf8 emits a BOM,
        # which strict XML parsers (Google Search Console included) reject as content-before-prolog.
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false

        # 1) Generate sitemap.xml locally
        $xml         = New-SitemapXml -Domain $s.Domain -Pages $published
        $sitemapFile = Join-Path $outDir "$($s.Key)-sitemap.xml"
        [System.IO.File]::WriteAllText($sitemapFile, $xml, $utf8NoBom)
        Write-Host "   wrote $sitemapFile"

        # 2) Generate robots.txt locally
        $robotsBody = "User-agent: *`nAllow: /`n`nSitemap: https://www.$($s.Domain)/sitemap.xml`n"
        $robotsFile = Join-Path $outDir "$($s.Key)-robots.txt"
        [System.IO.File]::WriteAllText($robotsFile, $robotsBody, $utf8NoBom)
        Write-Host "   wrote $robotsFile"

        if (-not $NoUpload) {
            $hostBucket  = "www.$($s.Domain)"
            $sitemapDest = "s3://$hostBucket/sitemap.xml"
            $robotsDest  = "s3://$hostBucket/robots.txt"

            Write-Host "   uploading $sitemapDest"
            aws s3 cp $sitemapFile $sitemapDest `
                --profile $AwsProfile `
                --region $Region `
                --content-type 'application/xml' `
                --cache-control 'public, max-age=3600' | Out-Null
            $sitemapOk = ($LASTEXITCODE -eq 0)
            if (-not $sitemapOk) { Write-Warning "   sitemap upload FAILED (exit $LASTEXITCODE)" }

            Write-Host "   uploading $robotsDest"
            aws s3 cp $robotsFile $robotsDest `
                --profile $AwsProfile `
                --region $Region `
                --content-type 'text/plain' `
                --cache-control 'public, max-age=3600' | Out-Null
            $robotsOk = ($LASTEXITCODE -eq 0)
            if (-not $robotsOk) { Write-Warning "   robots upload FAILED (exit $LASTEXITCODE)" }

            if ($sitemapOk -and $robotsOk) {
                Write-Host "   uploaded." -ForegroundColor Green
            }

            # 3) Best-effort CloudFront invalidation so the updated files are served immediately.
            if (-not $NoInvalidate) {
                $aliasWww  = "www.$($s.Domain)"
                $aliasBare = $s.Domain
                $jmesQuery = "DistributionList.Items[?Aliases.Items != null && (contains(Aliases.Items, '$aliasWww') || contains(Aliases.Items, '$aliasBare'))].Id"
                $distId = & aws cloudfront list-distributions `
                    --profile $AwsProfile `
                    --region $Region `
                    --query $jmesQuery `
                    --output text 2>$null
                if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($distId)) {
                    $distId = ($distId -split '\s+')[0]
                    Write-Host "   invalidating CloudFront distribution $distId for /sitemap.xml, /robots.txt"
                    aws cloudfront create-invalidation `
                        --distribution-id $distId `
                        --paths /sitemap.xml /robots.txt `
                        --profile $AwsProfile `
                        --region $Region | Out-Null
                    if ($LASTEXITCODE -eq 0) {
                        Write-Host "   invalidation submitted." -ForegroundColor Green
                    } else {
                        Write-Warning "   invalidation FAILED (exit $LASTEXITCODE)"
                    }
                } else {
                    Write-Warning "   no CloudFront distribution found with alias '$aliasWww' or '$aliasBare'; skipping invalidation"
                }
            }
        }

        $totals[$s.Key] = $published.Count
    }
    catch {
        Write-Warning "   $($s.Domain) failed: $_"
    }
}

# ----- Summary -----------------------------------------------------------
Write-Host ""
Write-Host '----- Summary -----' -ForegroundColor Yellow
foreach ($k in $totals.Keys | Sort-Object) {
    Write-Host ("   {0,-25} {1,5} pages in sitemap" -f $k, $totals[$k])
}
if ($NoUpload) {
    Write-Host ""
    Write-Host "(local-only run; sitemaps written to $outDir)" -ForegroundColor DarkGray
}
