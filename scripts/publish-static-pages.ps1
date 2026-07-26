<#
.SYNOPSIS
    Pre-render every CMS page to a static, crawlable HTML file and (optionally)
    upload it to the site's public S3 bucket.

.DESCRIPTION
    The public sites currently run as a client-rendered React SPA: every URL
    returns the same near-empty shell and the article text is painted in by
    JavaScript after load. Crawlers, social scrapers, and AI bots see nothing,
    so almost nothing is indexed.

    This script fixes that at the source. For every served page it:
      1. pulls the page + its articles from the CMS API,
      2. pulls the article HTML, site header, nav menu, and theme,
      3. assembles a COMPLETE static document -- real <title>, meta description,
         canonical, Open Graph/Twitter tags, JSON-LD, header/menu/theme, and the
         article body inlined in <body> (no JS required to see the content),
      4. writes it to a clean path key ({slug}/index.html).

    It mirrors regenerate-sitemaps.ps1 on purpose: same site table, same env
    vars, same "no Lambda, no new infrastructure" approach. The exact same
    assembly is intended to be ported into a C# StaticPageRenderer so the save
    hook produces byte-identical output -- this script is the reference render
    and the bulk-backfill tool.

.PARAMETER Site
    Site key (ldsapologetics, ldsdoctrines, ldsdiscussions, cesletter,
    ldsfaithincrisis, reflectiverealizations) or 'all'. Default: all.

.PARAMETER Slug
    Optional. Only render the single page whose CMS name matches this value
    (case-insensitive). Handy for eyeballing one page, e.g. -Slug Temple-And-Masonry.

.PARAMETER NoUpload
    Build the static files into .\dist\static\ locally without touching S3.
    This is the safe default for inspection.

.PARAMETER Upload
    Actually upload the generated files to the public bucket (www.{domain}).
    Nothing goes to S3 unless you pass this switch.

.PARAMETER AwsProfile
    AWS CLI profile for uploads. Default: tbirdcontractinggmailcom.

.NOTES
    Requires env vars (same as the MCP server / sitemap script):
        LAMBDA_API_BASE_URL   e.g. https://xxxx.execute-api.us-west-2.amazonaws.com/prod
        MCP_API_KEY           the CMS API key
#>

param(
    [ValidateSet('all', 'ldsapologetics', 'ldsdoctrines', 'ldsdiscussions', 'cesletter', 'ldsfaithincrisis', 'reflectiverealizations')]
    [string]$Site = 'all',
    [string]$Slug,
    [switch]$Upload,
    [switch]$NoUpload,
    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region = 'us-west-2'
)

# Upload only when explicitly asked. -NoUpload is accepted for symmetry but the
# default is already no-upload, so you never touch S3 by accident.
$doUpload = $Upload.IsPresent -and -not $NoUpload.IsPresent

$env:AWS_REGION = $Region
$env:AWS_DEFAULT_REGION = $Region

# ----- Site table (Id + public domain + S3 site folder name) -------------
# ContentBase is the raw content bucket URL the SPA already reads from
# (config.Site.appURL). SiteName is the DB website.name == S3 folder == domain.
$contentBase = 'https://www-websitecontent.s3.us-west-2.amazonaws.com'
$sites = @(
    @{ Key = 'ldsfaithincrisis';       Id = 1; Domain = 'ldsfaithincrisis.com';       Analytics = '' }
    @{ Key = 'ldsdoctrines';           Id = 2; Domain = 'ldsdoctrines.com';           Analytics = '' }
    @{ Key = 'reflectiverealizations'; Id = 4; Domain = 'reflectiverealizations.com'; Analytics = '' }
    @{ Key = 'ldsapologetics';         Id = 5; Domain = 'ldsapologetics.com';         Analytics = 'G-J6H714HFSM' }
    @{ Key = 'ldsdiscussions';         Id = 6; Domain = 'ldsdiscussions.info';        Analytics = '' }
    @{ Key = 'cesletter';              Id = 8; Domain = 'cesletter.info';             Analytics = '' }
)

# ----- Env -----------------------------------------------------------------
$apiBase = $env:LAMBDA_API_BASE_URL
$apiKey  = $env:MCP_API_KEY
if (-not $apiBase) { Write-Error 'LAMBDA_API_BASE_URL env var is not set.'; exit 1 }
if (-not $apiKey)  { Write-Error 'MCP_API_KEY env var is not set.'; exit 1 }
$apiBase = $apiBase.TrimEnd('/')
$apiHeaders = @{ 'Content-Type' = 'application/json'; 'X-API-Key' = $apiKey }

if ($doUpload) {
    if (-not (Get-Command aws -ErrorAction SilentlyContinue)) {
        Write-Warning 'AWS CLI not found; cannot upload. Run without -Upload for a local build.'
        exit 1
    }
}

# ----- Helpers -------------------------------------------------------------
# Windows PowerShell 5.1 decodes responses with no charset header as ISO-8859-1,
# which turns UTF-8 em dashes/curly quotes into mojibake. Always read the raw
# bytes and decode as UTF-8 ourselves.
function Invoke-Utf8 {
    param([string]$Uri, [string]$Method = 'Get', $Body = $null, [hashtable]$Headers = @{})
    $params = @{ Uri = $Uri; Method = $Method; Headers = $Headers; UseBasicParsing = $true }
    if ($null -ne $Body) { $params.Body = $Body }
    try {
        $r = Invoke-WebRequest @params
        $bytes = $r.RawContentStream.ToArray()
        return [System.Text.Encoding]::UTF8.GetString($bytes)
    } catch { return $null }
}

function Get-AllPages {
    param([int]$WebsiteId)
    $body = @{ Name = $null; Keywords = @(); Topics = @(); Description = $null; WebsiteId = $WebsiteId } | ConvertTo-Json -Depth 5
    $json = Invoke-Utf8 -Uri "$apiBase/page/search" -Method Post -Body $body -Headers $apiHeaders
    if (-not $json) { return @() }
    return $json | ConvertFrom-Json
}

function Get-PageById {
    param([int]$Id, [int]$WebsiteId)
    $json = Invoke-Utf8 -Uri "$apiBase/page/$Id/$WebsiteId" -Method Get -Headers $apiHeaders
    if (-not $json) { return $null }
    return $json | ConvertFrom-Json
}

function Get-Menu {
    param([int]$WebsiteId)
    $json = Invoke-Utf8 -Uri "$apiBase/menu/$WebsiteId" -Method Get -Headers $apiHeaders
    if (-not $json) { return @() }
    return $json | ConvertFrom-Json
}

function Get-ArticleContent {
    param([int]$Id)
    $t = Invoke-Utf8 -Uri "$apiBase/article/$Id/content" -Method Get -Headers $apiHeaders
    if (-not $t) { return '' }
    return $t
}

function Get-PublicText {
    param([string]$Url)
    $t = Invoke-Utf8 -Uri $Url -Method Get
    if (-not $t) { return '' }
    return $t
}

function Test-IsServedPage {
    param($Page)
    if (-not $Page) { return $false }
    if ([string]::IsNullOrWhiteSpace($Page.name)) { return $false }
    if (-not $Page.status) { return $true }
    return ($Page.status -eq 'published')
}

function ConvertTo-Slug {
    param([string]$Name)
    # Public path segment: lowercase, keep existing hyphens, strip anything unsafe.
    $s = $Name.Trim().ToLowerInvariant()
    $s = ($s -replace '\s+', '-')
    $s = ($s -replace '[^a-z0-9\-]', '')
    $s = ($s -replace '-{2,}', '-').Trim('-')
    return $s
}

function ConvertTo-LayoutClass {
    param([string]$Layout)
    if ([string]::IsNullOrWhiteSpace($Layout)) { return '' }
    $s = ($Layout -replace '\s+', '') -replace ',', '-' -replace '/', '_'
    $s = ($s -replace '(?i)Grid', '').ToLowerInvariant()
    return "layout-$s"
}

function Get-HtmlEncoded {
    param([string]$Text)
    if ([string]::IsNullOrEmpty($Text)) { return '' }
    return ($Text -replace '&', '&amp;' -replace '<', '&lt;' -replace '>', '&gt;' -replace '"', '&quot;')
}

function Get-FirstH1 {
    param([string]$Html)
    if ([string]::IsNullOrEmpty($Html)) { return '' }
    $m = [regex]::Match($Html, '(?is)<h1[^>]*>(.*?)</h1>')
    if ($m.Success) {
        $t = [regex]::Replace($m.Groups[1].Value, '(?s)<[^>]+>', '') # strip inner tags
        return $t.Trim()
    }
    return ''
}

function Get-MetaDescription {
    param([string]$PageDesc, [string]$Html)
    $d = $PageDesc
    if ([string]::IsNullOrWhiteSpace($d)) {
        # Fall back to first paragraph's text.
        $m = [regex]::Match($Html, '(?is)<p[^>]*>(.*?)</p>')
        if ($m.Success) { $d = [regex]::Replace($m.Groups[1].Value, '(?s)<[^>]+>', '') }
    }
    $d = ($d -replace '\s+', ' ').Trim()
    if ($d.Length -gt 160) { $d = $d.Substring(0, 157).TrimEnd() + '...' }
    return $d
}

function Build-MenuNav {
    param($Menu, [string]$Domain)
    if (-not $Menu -or $Menu.Count -eq 0) { return '' }
    $top = $Menu | Where-Object { $_.parent -eq 0 }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<nav class="menuContents" aria-label="Site navigation"><ul>')
    foreach ($section in $top) {
        $label = Get-HtmlEncoded $section.text
        [void]$sb.AppendLine("<li><span class=""nav-section"">$label</span>")
        $children = $Menu | Where-Object { $_.parent -eq $section.id -and $_.pageName }
        if ($children) {
            [void]$sb.AppendLine('<ul>')
            foreach ($child in $children) {
                $slug  = ConvertTo-Slug $child.pageName
                $ctext = Get-HtmlEncoded $child.text
                [void]$sb.AppendLine("<li><a href=""https://www.$Domain/$slug/"">$ctext</a></li>")
            }
            [void]$sb.AppendLine('</ul>')
        }
        [void]$sb.AppendLine('</li>')
    }
    [void]$sb.AppendLine('</ul></nav>')
    return $sb.ToString()
}

function New-StaticDocument {
    param(
        [hashtable]$SiteInfo,
        $Page,
        [string]$BodyHtml,      # assembled article(s) HTML
        [string]$HeaderHtml,
        [string]$NavHtml,
        [string]$ThemeUrl,
        [string]$Slug
    )
    $domain    = $SiteInfo.Domain
    $siteTitle = $SiteInfo.Title
    $isHome    = ($Slug -eq '')
    $canonical = if ($isHome) { "https://www.$domain/" } else { "https://www.$domain/$Slug/" }

    $h1    = Get-FirstH1 $BodyHtml
    $title = if ($h1) { $h1 } elseif ($Page.articles -and $Page.articles[0].name) { $Page.articles[0].name } else { ($Page.name -replace '-', ' ') }
    $desc  = Get-MetaDescription -PageDesc $Page.description -Html $BodyHtml

    $encTitle = Get-HtmlEncoded $title
    $encDesc  = Get-HtmlEncoded $desc
    $encSite  = Get-HtmlEncoded $siteTitle
    $ogImage  = ''
    if ($Page.articles -and $Page.articles[0].memeImagePath) {
        $ogImage = "$contentBase/public/$($Page.articles[0].memeImagePath)"
    }

    # JSON-LD (Article). No fabricated dates -- omit fields we can't verify.
    $jsonLd = @{
        '@context'         = 'https://schema.org'
        '@type'            = 'Article'
        'headline'         = $title
        'description'      = $desc
        'inLanguage'       = 'en'
        'mainEntityOfPage' = $canonical
        'url'              = $canonical
        'publisher'        = @{ '@type' = 'Organization'; 'name' = $siteTitle; 'url' = "https://www.$domain/" }
    } | ConvertTo-Json -Depth 6 -Compress

    $ga = ''
    if ($SiteInfo.Analytics) {
        $tag = $SiteInfo.Analytics
        $ga = @"
<script async src="https://www.googletagmanager.com/gtag/js?id=$tag"></script>
<script>window.dataLayer=window.dataLayer||[];function gtag(){dataLayer.push(arguments);}gtag('js',new Date());gtag('config','$tag');</script>
"@
    }

    $themeLink = if ($ThemeUrl) { "<link rel=""stylesheet"" href=""$ThemeUrl"">" } else { '' }
    $ogImageTag = if ($ogImage) { "<meta property=""og:image"" content=""$ogImage"">" } else { '' }

    $doc = @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>$encTitle | $encSite</title>
<meta name="description" content="$encDesc">
<link rel="canonical" href="$canonical">
<meta name="robots" content="index, follow">
<meta property="og:type" content="article">
<meta property="og:title" content="$encTitle">
<meta property="og:description" content="$encDesc">
<meta property="og:url" content="$canonical">
<meta property="og:site_name" content="$encSite">
$ogImageTag
<meta name="twitter:card" content="summary_large_image">
<meta name="twitter:title" content="$encTitle">
<meta name="twitter:description" content="$encDesc">
$themeLink
<script type="application/ld+json">$jsonLd</script>
$ga
</head>
<body>
<div class="pageContents">
<header class="headerContents">$HeaderHtml</header>
$NavHtml
<main class="articleContents $(ConvertTo-LayoutClass $Page.layout)">
$BodyHtml
</main>
</div>
</body>
</html>
"@
    return $doc
}

# ----- Output dir ----------------------------------------------------------
$outRoot = Join-Path $PSScriptRoot '..\dist\static'
New-Item -ItemType Directory -Force -Path $outRoot | Out-Null
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

# ----- Select sites --------------------------------------------------------
$selected = if ($Site -eq 'all') { $sites } else { $sites | Where-Object { $_.Key -eq $Site } }
if (-not $selected) { Write-Error "Unknown site key: $Site"; exit 1 }

# ----- Main ----------------------------------------------------------------
foreach ($s in $selected) {
    Write-Host ""
    Write-Host "==> $($s.Domain)  (WebsiteId=$($s.Id))" -ForegroundColor Cyan

    $siteName  = $s.Domain           # DB website.name == S3 folder == domain
    $siteInfo  = @{ Domain = $s.Domain; Title = 'LDS Apologetics'; Analytics = $s.Analytics }
    if ($s.Key -eq 'ldsapologetics') { $siteInfo.Title = 'LDS Apologetics' }

    # Site-wide assets (fetched once): header, theme, menu.
    $headerHtml = Get-PublicText "$contentBase/public/websites/$siteName/header.html"
    $themeUrl   = "$contentBase/public/assets/$siteName/themes/theme.css"
    $menu       = Get-Menu -WebsiteId $s.Id
    $navHtml    = Build-MenuNav -Menu $menu -Domain $s.Domain
    if (-not $headerHtml) { Write-Warning "   header.html not found (page will render without header)" }

    $all = Get-AllPages -WebsiteId $s.Id
    $served = @($all | Where-Object { Test-IsServedPage $_ })
    if ($Slug) { $served = @($served | Where-Object { $_.name -ieq $Slug }) }
    Write-Host ("   {0} served pages{1}" -f $served.Count, $(if ($Slug) { " (filtered to '$Slug')" } else { '' }))

    $siteOut = Join-Path $outRoot $s.Key
    $count = 0
    foreach ($p in $served) {
        try {
            $full = Get-PageById -Id $p.id -WebsiteId $s.Id
            $arts = @($full.articles | Sort-Object sequence_no)

            # Assemble the article body (each article wrapped like PageContainer does).
            $bodySb = New-Object System.Text.StringBuilder
            foreach ($a in $arts) {
                if ($a.articlePath) {
                    $html = Get-ArticleContent -Id $a.id
                    if ($html) { [void]$bodySb.AppendLine("<article>$html</article>") }
                }
            }
            $bodyHtml = $bodySb.ToString()
            if (-not $bodyHtml.Trim()) { Write-Warning "   [$($p.name)] no article content; skipped"; continue }

            $slug = if ($p.name -ieq 'Home') { '' } else { ConvertTo-Slug $p.name }
            $doc  = New-StaticDocument -SiteInfo $siteInfo -Page $full -BodyHtml $bodyHtml `
                        -HeaderHtml $headerHtml -NavHtml $navHtml -ThemeUrl $themeUrl -Slug $slug

            $destDir  = if ($slug -eq '') { $siteOut } else { Join-Path $siteOut $slug }
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
            $destFile = Join-Path $destDir 'index.html'
            [System.IO.File]::WriteAllText($destFile, $doc, $utf8NoBom)
            $count++

            if ($doUpload) {
                $key = if ($slug -eq '') { 'index.html' } else { "$slug/index.html" }
                aws s3 cp $destFile "s3://www.$($s.Domain)/$key" `
                    --profile $AwsProfile --region $Region `
                    --content-type 'text/html; charset=utf-8' `
                    --cache-control 'public, max-age=300' | Out-Null
            }
        }
        catch {
            Write-Warning "   [$($p.name)] failed: $_"
        }
    }
    Write-Host ("   wrote {0} static pages to {1}" -f $count, $siteOut) -ForegroundColor Green
}

Write-Host ""
if (-not $doUpload) {
    Write-Host "(local-only build; pass -Upload to push to S3)" -ForegroundColor DarkGray
}
