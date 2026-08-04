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
    ldsfaithincrisis) or 'all'. Default: all.
    reflectiverealizations is excluded on purpose -- see sites.json.

.PARAMETER Slug
    Optional. Only render the single page whose CMS name matches this value
    (case-insensitive). Handy for eyeballing one page, e.g. -Slug Temple-And-Masonry.

.PARAMETER ExcludeHome
    Skip the Home page. Home renders to the BUCKET ROOT index.html -- the object
    that currently holds the live SPA shell -- so publishing it is the one visible,
    destructive step in the whole migration. Everything else writes to a new
    {slug}/index.html key and cannot affect the running site.

    Pass it whenever you are re-rendering an existing site in bulk and do not
    intend to touch the homepage. Omit it only when you actually mean to publish
    Home -- for a brand-new site, or after deliberately changing the home page.

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
    [ValidateSet('all', 'ldsapologetics', 'ldsdoctrines', 'ldsdiscussions', 'cesletter', 'ldsfaithincrisis', 'ldsgospeldoctrine')]
    [string]$Site = 'all',
    [string]$Slug,
    [switch]$Upload,
    [switch]$NoUpload,
    [switch]$ExcludeHome,
    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region = 'us-west-2'
)

# Upload only when explicitly asked. -NoUpload is accepted for symmetry but the
# default is already no-upload, so you never touch S3 by accident.
$doUpload = $Upload.IsPresent -and -not $NoUpload.IsPresent

# Pages that errored, across every site in this run. The exit code is derived from this
# at the end -- otherwise the script inherits $LASTEXITCODE from whatever `aws` call ran
# last, which for a site with no theme.css is a failed (and entirely expected) probe. A
# successful run reporting "exit 1" teaches you to ignore exit codes.
$script:FailedPages = 0

$env:AWS_REGION = $Region
$env:AWS_DEFAULT_REGION = $Region

# ----- Site table (Id + public domain + S3 site folder name) -------------
# ContentBase is the raw content bucket URL the SPA already reads from
# (config.Site.appURL). SiteName is the DB website.name == S3 folder == domain.
$contentBase   = 'https://www-websitecontent.s3.us-west-2.amazonaws.com'
$contentBucket = 'www-websitecontent'   # read with credentials -- it is NOT public

# Structural CSS shared with StaticPageRenderer (which embeds the same file). This is the
# single source of truth for layout grids, .articleContents padding, and menu styling --
# all of which live only in the SPA bundle and would otherwise be absent from static pages.
$layoutCssPath = Join-Path (Split-Path $PSScriptRoot -Parent) 'infra\static\page-layout.css'
if (-not (Test-Path $layoutCssPath)) {
    throw "Structural stylesheet not found at $layoutCssPath. Rendering without it would silently strip every page's layout, padding and navigation styling."
}
$script:layoutCss = (Get-Content $layoutCssPath -Raw)

$sites = @(
    # Analytics ids MUST match react/baseProject/configs/{site}-Prod.json (analyticsTag)
    # and StaticPageRenderer.SiteMetaById. A blank drops GA for that site once it is static.
    @{ Key = 'ldsfaithincrisis';       Id = 1; Domain = 'ldsfaithincrisis.com';       Analytics = 'G-3X09PX2WS1'; Title = 'LDS Faith in Crisis' }
    @{ Key = 'ldsdoctrines';           Id = 2; Domain = 'ldsdoctrines.com';           Analytics = 'G-PY9E2KT5DC';  Title = 'LDS Doctrines' }
    # reflectiverealizations (id 4) is deliberately absent: it is a hand-built static
    # site, not the SPA, and rendering its single 2KB CMS article over the live
    # homepage would destroy it. See "excluded" in sites.json.
    @{ Key = 'ldsapologetics';         Id = 5; Domain = 'ldsapologetics.com';         Analytics = 'G-J6H714HFSM';  Title = 'LDS Apologetics' }
    @{ Key = 'ldsdiscussions';         Id = 6; Domain = 'ldsdiscussions.info';        Analytics = 'G-G8VH9TBRNR';  Title = 'LDS Discussions' }
    @{ Key = 'cesletter';              Id = 8; Domain = 'cesletter.info';             Analytics = 'G-Z4XDMTTGRN';  Title = 'CES Letter' }
    @{ Key = 'ldsgospeldoctrine';      Id = 9; Domain = 'ldsgospeldoctrine.info';     Analytics = 'G-0QJ9ZJ0LC6'; Title = 'LDS Gospel Doctrine' }
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

# Repairs in-body links whose leading slash was stripped.
#
# The admin editor (TinyMCE) ran for a long time on its default convert_urls /
# relative_urls settings, which rewrite an authored "/some-page/" into a path relative to
# the page being edited -- "some-page/", or "../../some-page/". Pages are served at
# /{slug}/, so a relative href resolves UNDERNEATH the current article and 404s. Nothing
# surfaces it: the nav is rebuilt from sitemenu.json with absolute URLs on every render,
# so only article bodies rot.
#
# StaticPageRenderer.NormalizeInternalLinks does exactly this inside the Lambda. This
# script is the bulk-backfill twin of that renderer, so it MUST apply the same repair --
# a backfill run with the unpatched logic would silently re-break every page it wrote,
# undoing the Lambda's work. Keep the two in step; the C# side is pinned by
# StaticRenderLinkTests.cs.
#
# Left strictly alone -- these must never be rewritten:
#   * anything carrying a URI scheme (http:, https:, mailto:, tel:, data:, javascript:)
#   * protocol-relative "//host/path", which is external
#   * fragment-only "#section" and query-only "?x=1"
#   * hrefs already rooted at "/"
function ConvertTo-RootedLinks {
    param([string]$Html)

    if ([string]::IsNullOrEmpty($Html)) { return $Html }
    if ($Html.IndexOf('href', [System.StringComparison]::OrdinalIgnoreCase) -lt 0) { return $Html }

    $evaluator = {
        param($m)

        $single = $m.Groups[4].Success
        if ($single) { $href = $m.Groups[4].Value } else { $href = $m.Groups[3].Value }

        if ([string]::IsNullOrWhiteSpace($href)) { return $m.Value }

        $h = $href.Trim()

        # A leading '/' covers both site-rooted "/page/" and protocol-relative "//host".
        if ($h[0] -eq '/' -or $h[0] -eq '#' -or $h[0] -eq '?') { return $m.Value }

        # Any URI scheme at all.
        if ($h -match '^[a-zA-Z][a-zA-Z0-9+.\-]*:') { return $m.Value }

        # Whatever "./" or "../" prefix the editor introduced is not the author's intent.
        $trimmed = [regex]::Replace($h, '^(?:\.{1,2}/)+', '')
        if ($trimmed.Length -eq 0) { return $m.Value }

        if ($single) { $q = "'" } else { $q = '"' }
        return $m.Groups[1].Value + $q + '/' + $trimmed + $q
    }

    return [regex]::Replace($Html, '(?is)(<a\b[^>]*?\bhref\s*=\s*)("([^"]*)"|''([^'']*)'')', $evaluator)
}

# Read a text object out of the CONTENT bucket using AWS credentials.
#
# This used to fetch over plain HTTP from the bucket's public URL. That bucket is not
# publicly readable -- every request 403s -- and the failure was swallowed, so pages
# rendered with no header and no theme and the script reported success. Use the CLI
# (credentialed) and surface a real failure instead.
function Get-BucketText {
    param(
        [Parameter(Mandatory)][string]$Bucket,
        [Parameter(Mandatory)][string]$Key,
        [switch]$Quiet
    )
    $tmp = [System.IO.Path]::GetTempFileName()
    try {
        & aws s3 cp "s3://$Bucket/$Key" $tmp --profile $AwsProfile --region $Region --only-show-errors 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            if (-not $Quiet) { Write-Warning "   could not read s3://$Bucket/$Key" }
            return ''
        }
        return [System.IO.File]::ReadAllText($tmp, [System.Text.Encoding]::UTF8)
    }
    catch {
        if (-not $Quiet) { Write-Warning "   could not read s3://$Bucket/$Key : $_" }
        return ''
    }
    finally { Remove-Item $tmp -Force -ErrorAction SilentlyContinue }
}

# Convenience wrapper for the CONTENT bucket.
function Get-ContentText {
    param(
        [Parameter(Mandatory)][string]$Key,     # e.g. public/websites/cesletter.info/header.html
        [switch]$Quiet
    )
    return Get-BucketText -Bucket $contentBucket -Key $Key -Quiet:$Quiet
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

# Markup -> plain text: strip tags, THEN resolve entities.
#
# The decode matters. Article HTML follows the house style in CLAUDE.md, which uses
# &mdash; / &ldquo; / &rdquo; liberally. Stripping tags leaves those as literal text and
# the later HTML-encode escapes their ampersand, so an <h1> of "A &mdash; B" shipped a
# <title> of "A &amp;mdash; B" -- which renders as the visible characters "A &mdash; B"
# in the browser tab and in search results. Decoding first means the encoder sees a real
# em dash, which needs no escaping.
#
# MUST MIRROR StaticPageRenderer.PlainText in the C# renderer. The Lambda re-renders on
# every article save; this script does the bulk backfill. If the two disagree, pages
# silently change appearance depending on which one last touched them.
function ConvertTo-PlainText {
    param([string]$Markup)
    if ([string]::IsNullOrEmpty($Markup)) { return '' }
    $stripped = [regex]::Replace($Markup, '(?s)<[^>]+>', '')
    return [System.Net.WebUtility]::HtmlDecode($stripped)
}

function Get-FirstH1 {
    param([string]$Html)
    if ([string]::IsNullOrEmpty($Html)) { return '' }
    $m = [regex]::Match($Html, '(?is)<h1[^>]*>(.*?)</h1>')
    if ($m.Success) { return (ConvertTo-PlainText $m.Groups[1].Value).Trim() }
    return ''
}

function Get-MetaDescription {
    param([string]$PageDesc, [string]$Html)
    $d = $PageDesc
    if ([string]::IsNullOrWhiteSpace($d)) {
        # Fall back to first paragraph's text.
        $m = [regex]::Match($Html, '(?is)<p[^>]*>(.*?)</p>')
        if ($m.Success) { $d = $m.Groups[1].Value }
    }
    # Applied to the CMS description too -- descriptions are authored alongside the HTML
    # and pick up the same entities. Decode BEFORE truncating, or "&mdash;" spends 7 of
    # the 160 characters instead of 1.
    $d = ConvertTo-PlainText $d
    $d = ($d -replace '\s+', ' ').Trim()
    if ($d.Length -gt 160) {
        # Truncate on a word boundary -- cutting mid-word produced live descriptions
        # ending "...f...".
        $cut = $d.Substring(0, 157)
        $lastSpace = $cut.LastIndexOf(' ')
        if ($lastSpace -gt 100) { $cut = $cut.Substring(0, $lastSpace) }
        $d = $cut.TrimEnd(' ', ',', ';', ':', '-', [char]0x2014) + '...'
    }
    return $d
}

# Menu items are NOT uniformly shaped. An item that is only a group heading has no
# pageName property AT ALL -- not null, absent -- and cesletter's menu is 10 of them.
# Under Set-StrictMode, asking for an absent property is a terminating error, so every
# read of a menu field has to go through here. Mirrors the null-tolerant JToken indexer
# the C# renderer gets for free.
function Get-MenuField {
    param($Item, [string]$Name)
    if ($null -eq $Item) { return $null }
    $prop = $Item.PSObject.Properties[$Name]
    if (-not $prop) { return $null }
    return $prop.Value
}

# react-dnd-treeview stores custom fields under `data` in some menus and at the top level
# in others. StaticPageRenderer.BuildNav checks both, so this must too or the two renderers
# disagree about which items are links.
function Get-MenuPageName {
    param($Item)
    $direct = Get-MenuField $Item 'pageName'
    if (-not [string]::IsNullOrWhiteSpace($direct)) { return $direct }
    $nested = Get-MenuField (Get-MenuField $Item 'data') 'pageName'
    if (-not [string]::IsNullOrWhiteSpace($nested)) { return $nested }
    return $null
}

function Build-MenuNav {
    param($Menu, [string]$Domain)
    if (-not $Menu -or $Menu.Count -eq 0) { return '' }
    $top = $Menu | Where-Object { "$(Get-MenuField $_ 'parent')" -eq '0' }
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<nav class="menuContents" aria-label="Site navigation"><ul>')
    # A menu item that points at a page must be a LINK, at any level. Home lives at the
    # site root, not /home/. Must match StaticPageRenderer.BuildNav exactly.
    function Get-MenuHref {
        param([string]$PageName, [string]$Domain)
        if ($PageName -ieq 'Home') { return "https://www.$Domain/" }
        return "https://www.$Domain/$(ConvertTo-Slug $PageName)/"
    }

    foreach ($section in $top) {
        $label = Get-HtmlEncoded (Get-MenuField $section 'text')

        # Top-level items are frequently pages in their own right, not just group
        # headings. Rendering those as a bare <span> makes the whole nav unclickable.
        $sectionPage = Get-MenuPageName $section
        if ($sectionPage) {
            [void]$sb.AppendLine("<li><a href=""$(Get-MenuHref -PageName $sectionPage -Domain $Domain)"">$label</a>")
        } else {
            [void]$sb.AppendLine("<li><span class=""nav-section"">$label</span>")
        }

        $sectionId = Get-MenuField $section 'id'
        $children = @($Menu | Where-Object {
            "$(Get-MenuField $_ 'parent')" -eq "$sectionId" -and (Get-MenuPageName $_)
        })
        if ($children.Count -gt 0) {
            [void]$sb.AppendLine('<ul>')
            foreach ($child in $children) {
                $ctext = Get-HtmlEncoded (Get-MenuField $child 'text')
                [void]$sb.AppendLine("<li><a href=""$(Get-MenuHref -PageName (Get-MenuPageName $child) -Domain $Domain)"">$ctext</a></li>")
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
        [string]$ThemeCss,      # theme.css CONTENT, inlined (not a URL) -- matches StaticPageRenderer
        [string]$BaseStyles,    # BaseStyles.css CONTENT -- the per-site brand stylesheet
        [string]$Slug
    )
    $domain    = $SiteInfo.Domain
    $siteTitle = $SiteInfo.Title
    $isHome    = ($Slug -eq '')
    $canonical = if ($isHome) { "https://www.$domain/" } else { "https://www.$domain/$Slug/" }

    # Title precedence: visible <h1>, then the PAGE name, then an article name.
    # Page name before article name is deliberate -- page names are human-meaningful and
    # match the URL ("About-Us" -> "About Us"); article names are internal CMS labels.
    # Must match StaticPageRenderer.BuildDocument exactly.
    $h1        = Get-FirstH1 $BodyHtml
    $pageTitle = ($Page.name -replace '-', ' ').Trim()
    $title = if ($h1) { $h1 }
             elseif ($pageTitle) { $pageTitle }
             elseif ($Page.articles -and $Page.articles[0].name) { $Page.articles[0].name }
             else { '' }
    $desc  = Get-MetaDescription -PageDesc $Page.description -Html $BodyHtml

    $encTitle = Get-HtmlEncoded $title
    $encDesc  = Get-HtmlEncoded $desc
    $encSite  = Get-HtmlEncoded $siteTitle
    $ogImage  = ''
    if ($Page.articles -and $Page.articles[0].memeImagePath) {
        $ogImage = "$contentBase/public/$($Page.articles[0].memeImagePath)"
    }

    # JSON-LD (Article). No fabricated dates -- omit fields we can't verify.
    # [ordered] is load-bearing: a plain @{} hashtable serialises in an arbitrary order, so
    # this block came out shuffled relative to StaticPageRenderer's, and every page then
    # differed from its C#-rendered twin for no reason. Key order is meaningless to a
    # consumer, but it makes "diff the local render against the live object" -- the cheapest
    # check there is that the two renderers still agree -- useless.
    $jsonLd = [ordered]@{
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

    # Structural CSS (layout grids, .articleContents padding, menu) comes from the shared
    # infra/static/page-layout.css -- the same file StaticPageRenderer embeds. Without it a
    # static page loses all of that silently, because those rules live only in the SPA
    # bundle, which a static page never loads.
    #
    # It goes BEFORE the theme so a site theme can still override structural defaults.
    $layoutTag = "<style>$script:layoutCss</style>"

    # Cascade order, least specific first: structural layout -> per-site BaseStyles.css ->
    # theme.css. All INLINED, matching StaticPageRenderer (locked decision: pages are
    # self-contained). They must match, or a page rendered by this backfill would visibly
    # change the first time an edit triggered the C# save-hook re-render.
    $baseTag  = if ($BaseStyles) { "<style>$BaseStyles</style>" } else { '' }
    $themeTag = if ($ThemeCss)   { "<style>$ThemeCss</style>"   } else { '' }
    $ogImageTag = if ($ogImage) { "<meta property=""og:image"" content=""$ogImage"">" } else { '' }

    # Home titles as the site itself -- "Home | Site" wastes the most valuable
    # characters in the SERP. Matches StaticPageRenderer.
    $titleTag = if ($isHome -and -not $h1) { "<title>$encSite</title>" } else { "<title>$encTitle | $encSite</title>" }

    $doc = @"
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
$titleTag
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
$layoutTag
$baseTag
$themeTag
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
    $siteInfo  = @{ Domain = $s.Domain; Title = $s.Title; Analytics = $s.Analytics }

    # Per-site title and GA tag come from the content bucket, exactly as
    # StaticPageRenderer.ResolveSiteMetaAsync reads them. The table above is only a fallback
    # for sites with no metadata file yet. If these two disagree, a page rendered here and
    # the same page rendered by the save-trigger differ in <title> and analytics -- the kind
    # of drift that shows up as "the tag disappeared" weeks later.
    $metaJson = Get-ContentText "public/websites/$siteName/site-meta.json" -Quiet
    if ($metaJson) {
        try {
            $meta = $metaJson | ConvertFrom-Json
            if ($meta.PSObject.Properties['title'] -and -not [string]::IsNullOrWhiteSpace($meta.title)) {
                $siteInfo.Title = $meta.title
            }
            # An explicit "" means "deliberately no analytics"; only an absent key falls back.
            if ($meta.PSObject.Properties['analytics']) { $siteInfo.Analytics = "$($meta.analytics)" }
            Write-Host ("   site-meta.json: title='{0}' analytics='{1}'" -f $siteInfo.Title, $siteInfo.Analytics) -ForegroundColor DarkGray
        } catch {
            Write-Warning "   site-meta.json is not valid JSON; using the built-in defaults"
        }
    }

    # Site-wide assets (fetched once): header, theme, menu.
    # Header comes from the CONTENT bucket only, because that is the single place the SPA
    # looks (PageGallery -> FileProcessing.fileExists('websites/{site}', headerFileName)).
    # A site with no header there genuinely has no header -- cesletter.info is one -- and
    # the static page must render the same. An earlier version fell back to a header.html
    # left at the public bucket root by an old build, which put a banner on cesletter that
    # the live site never had.
    $headerHtml = Get-ContentText "public/websites/$siteName/header.html" -Quiet

    # BaseStyles.css is the site's REAL stylesheet -- body font/colour/line-height plus the
    # .headerContents/.menuContents layout -- and it is compiled into the SPA bundle. Three
    # sites (ldsapologetics, ldsdoctrines, cesletter) have no theme.css at all, so this is
    # their ONLY site-wide CSS. The SPA build publishes it to the public bucket root.
    $baseStyles = Get-BucketText -Bucket "www.$($s.Domain)" -Key 'BaseStyles.css' -Quiet

    # theme.css is the ThemeBuilder output. Legitimately absent for several sites.
    $themeCss = Get-ContentText "public/assets/$siteName/themes/theme.css" -Quiet

    Write-Host ("   assets: header={0}  BaseStyles={1}  theme={2}" -f `
        $(if ($headerHtml) { "$($headerHtml.Length)B" } else { 'MISSING' }),
        $(if ($baseStyles) { "$($baseStyles.Length)B" } else { 'MISSING' }),
        $(if ($themeCss)   { "$($themeCss.Length)B"   } else { 'none' })) -ForegroundColor DarkGray

    # Refuse to PUBLISH a site with no site-wide CSS at all. Rendering would still
    # "succeed" and produce a full set of unstyled pages -- the kind of failure that looks
    # fine in the log and terrible in a browser. A missing theme.css on its own is fine:
    # several sites genuinely have none and are styled by BaseStyles.css.
    # A missing header is NOT a blocker: cesletter.info deliberately has none, and its SPA
    # renders without one, so blocking here would refuse to publish a correct site.
    # A local -NoUpload render is always allowed, so you can inspect the damage.
    if ($doUpload) {
        $blockers = @()
        if (-not $baseStyles -and -not $themeCss)  { $blockers += 'no BaseStyles.css AND no theme.css -- pages would be unstyled' }
        if ($blockers.Count -gt 0) {
            Write-Warning "   SKIPPING UPLOAD for $($s.Domain): $($blockers -join '; ')."
            Write-Warning "   Nothing was uploaded for this site. Fix the assets and re-run."
            continue
        }
    }
    $menu       = Get-Menu -WebsiteId $s.Id
    $navHtml    = Build-MenuNav -Menu $menu -Domain $s.Domain
    if (-not $headerHtml) { Write-Warning "   header.html not found (page will render without header)" }

    $all = Get-AllPages -WebsiteId $s.Id
    $served = @($all | Where-Object { Test-IsServedPage $_ })
    if ($Slug) { $served = @($served | Where-Object { $_.name -ieq $Slug }) }
    Write-Host ("   {0} served pages{1}" -f $served.Count, $(if ($Slug) { " (filtered to '$Slug')" } else { '' }))

    $siteOut = Join-Path $outRoot $s.Key
    $count = 0
    $skipped = 0
    foreach ($p in $served) {
        # Home writes to the bucket ROOT index.html, replacing the live SPA shell.
        # That is the one destructive write in this script, so it can be held back.
        if ($ExcludeHome -and $p.name -ieq 'Home') {
            Write-Host "   skipping Home (-ExcludeHome; it would overwrite the site root)" -ForegroundColor DarkGray
            continue
        }
        try {
            $full = Get-PageById -Id $p.id -WebsiteId $s.Id
            $arts = @($full.articles | Sort-Object sequence_no)

            # Assemble the article body (each article wrapped like PageContainer does).
            $bodySb = New-Object System.Text.StringBuilder
            foreach ($a in $arts) {
                if ($a.articlePath) {
                    $html = Get-ArticleContent -Id $a.id
                    # Must match StaticPageRenderer.NormalizeInternalLinks -- see above.
                    if ($html) {
                        $html = ConvertTo-RootedLinks -Html $html
                        [void]$bodySb.AppendLine("<article>$html</article>")
                    }
                }
            }
            $bodyHtml = $bodySb.ToString()
            if (-not $bodyHtml.Trim()) { Write-Warning "   [$($p.name)] no article content; skipped"; $skipped++; continue }

            $slug = if ($p.name -ieq 'Home') { '' } else { ConvertTo-Slug $p.name }
            $doc  = New-StaticDocument -SiteInfo $siteInfo -Page $full -BodyHtml $bodyHtml `
                        -HeaderHtml $headerHtml -NavHtml $navHtml -ThemeCss $themeCss -BaseStyles $baseStyles -Slug $slug

            $destDir  = if ($slug -eq '') { $siteOut } else { Join-Path $siteOut $slug }
            New-Item -ItemType Directory -Force -Path $destDir | Out-Null
            $destFile = Join-Path $destDir 'index.html'
            [System.IO.File]::WriteAllText($destFile, $doc, $utf8NoBom)

            if ($doUpload) {
                $key = if ($slug -eq '') { 'index.html' } else { "$slug/index.html" }
                aws s3 cp $destFile "s3://www.$($s.Domain)/$key" `
                    --profile $AwsProfile --region $Region `
                    --content-type 'text/html; charset=utf-8' `
                    --cache-control 'public, max-age=300' | Out-Null
                # An unchecked upload is the worst failure this script can have: the local
                # render succeeded, so the page is counted and reported as published while
                # the bucket still holds nothing. Expired credentials mid-run look exactly
                # like this.
                if ($LASTEXITCODE -ne 0) {
                    throw "upload to s3://www.$($s.Domain)/$key failed (aws exit $LASTEXITCODE)"
                }
            }
            $count++
        }
        catch {
            Write-Warning "   [$($p.name)] failed: $_"
            $script:FailedPages++
        }
    }
    Write-Host ("   wrote {0} static pages to {1}" -f $count, $siteOut) -ForegroundColor Green
    if ($skipped -gt 0) { Write-Host ("   {0} page(s) skipped for having no article content" -f $skipped) -ForegroundColor Yellow }
}

Write-Host ""
if (-not $doUpload) {
    Write-Host "(local-only build; pass -Upload to push to S3)" -ForegroundColor DarkGray
}

if ($script:FailedPages -gt 0) {
    Write-Host ("{0} page(s) FAILED -- see the warnings above." -f $script:FailedPages) -ForegroundColor Red
    exit 1
}
exit 0
