<#
.SYNOPSIS
    Read-only health check for a live static site. Changes NOTHING.
    Run it after a content batch, before a release, or whenever something looks off.

.DESCRIPTION
    This began as the pre-cutover go/no-go check for the 2026 static migration. That
    migration is finished, but the checks turned out to be worth more afterwards than
    during: every one of them found a real defect on a site that looked perfectly fine
    from a browser, and each failure mode is silent -- the pages themselves still serve.

    What it catches that nothing else does:
      * a page name that slugs to the same path as another (they overwrite each other;
        for "Home" that is the BUCKET ROOT, so the live homepage becomes a coin flip)
      * a menu item pointing at a page that does not exist -- a dead nav link rendered
        into EVERY page of the site
      * the database menu and the S3 sitemenu.json drifting apart, so the published nav
        and the nav the admin shows you disagree
      * a distribution pointing at the S3 REST endpoint instead of the website endpoint,
        which 403s every clean URL
      * pages in the CMS with no corresponding object in the bucket

    Checks:
      1. Tooling + credentials  -- aws cli, node, dotnet, AWS session, CMS env vars
      2. CMS reachable          -- page count for the site
      3. Slug parity            -- runs the REAL CloudFront function against EVERY
                                   live page name and compares its 301 target to
                                   where the renderer actually writes the file. A
                                   mismatch means legacy links 301 into a 404.
      4. CloudFront function    -- unit tests pass
      5. Public bucket          -- exists, website hosting on, root index.html
                                   present, what its cache-control is
      6. Public distribution    -- found, current function associations, whether
                                   the clean-URL function is already attached
      7. Static page coverage   -- how many of the site's pages already have a
                                   /slug/index.html object in the bucket
      8. Admin subdomain        -- bucket / cert / distribution / DNS status

.PARAMETER Site
    Site key from sites.json.

.PARAMETER AwsProfile
    AWS CLI profile. Default: tbirdcontractinggmailcom.

.PARAMETER SkipAws
    Only run the checks that need no AWS calls (tooling, CMS, slug parity, tests).
    Useful before you re-authenticate.

.EXAMPLE
    ./scripts/check-site-health.ps1 -Site ldsapologetics

.EXAMPLE
    # content-only checks, no AWS calls
    ./scripts/check-site-health.ps1 -Site ldsdoctrines -SkipAws
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [string]$Site,
    [string]$AwsProfile = 'tbirdcontractinggmailcom',
    [string]$Region     = 'us-west-2',
    [switch]$SkipAws
)

$ErrorActionPreference = 'Continue'   # a failed check must not abort the report
Set-StrictMode -Version Latest

. "$PSScriptRoot\lib\SiteInfra.ps1"
. "$PSScriptRoot\lib\GoDaddyDns.ps1"

$siteInfo   = Resolve-Site -Site $Site
$domain     = $siteInfo.domain
$publicHost = "www.$domain"
$adminHost  = "admin.$domain"
$pubBucket  = "www.$domain"
$repoRoot   = Split-Path $PSScriptRoot -Parent

$results = @()
function Add-Result {
    param([string]$Check, [ValidateSet('PASS', 'WARN', 'FAIL', 'INFO')][string]$Status, [string]$Detail)
    $script:results += [pscustomobject]@{ Check = $Check; Status = $Status; Detail = $Detail }
    $color = switch ($Status) { 'PASS' { 'Green' } 'WARN' { 'Yellow' } 'FAIL' { 'Red' } default { 'Gray' } }
    Write-Host ("  {0,-6} {1,-32} {2}" -f $Status, $Check, $Detail) -ForegroundColor $color
}

Write-Host "`nSite health: $($siteInfo.key)  ($domain)" -ForegroundColor White
Write-Host ('=' * 78)

# ------------------------------------------------------------------ 1. tooling
Write-Host "`n1. Tooling and credentials" -ForegroundColor Cyan

foreach ($tool in @(@{n='aws';c='--version'}, @{n='node';c='--version'}, @{n='dotnet';c='--version'})) {
    # Resolve first: piping a native command into Select-Object -First 1 terminates
    # the pipeline early and leaves $LASTEXITCODE unreliable.
    $cmd = Get-Command $tool.n -ErrorAction SilentlyContinue
    if (-not $cmd) { Add-Result $tool.n 'FAIL' 'not found on PATH'; continue }
    $out = & $tool.n $tool.c 2>&1
    $first = @($out)[0]
    Add-Result $tool.n 'PASS' $first
}

if ($env:LAMBDA_API_BASE_URL) { Add-Result 'LAMBDA_API_BASE_URL' 'PASS' $env:LAMBDA_API_BASE_URL }
else { Add-Result 'LAMBDA_API_BASE_URL' 'FAIL' 'not set -- content scripts cannot run' }

if ($env:MCP_API_KEY) { Add-Result 'MCP_API_KEY' 'PASS' 'set' }
else { Add-Result 'MCP_API_KEY' 'FAIL' 'not set -- content scripts cannot run' }

# GoDaddy DNS credential -- checked here, in daylight, because a bad scope and a
# bad account tier fail identically halfway through a cutover.
switch (Get-GoDaddyAuthKind) {
    'pat'     { Add-Result 'GoDaddy credential' 'PASS' 'GODADDY_PAT (Personal Access Token)' }
    'sso-key' { Add-Result 'GoDaddy credential' 'WARN' 'legacy key/secret -- works on v1 but retired after 2026; move to GODADDY_PAT' }
    default   { Add-Result 'GoDaddy credential' 'WARN' 'none set -- DNS records must be added by hand (workable, just slower)' }
}
if ((Get-GoDaddyAuthKind) -ne 'none') {
    $gd = Test-GoDaddyAccess -Domain $domain
    Add-Result 'GoDaddy API access' $(if ($gd.Ok) { 'PASS' } else { 'FAIL' }) $gd.Detail
}

$awsOk = $false
if (-not $SkipAws) {
    try {
        $id = Invoke-Aws @('sts', 'get-caller-identity', '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
        Add-Result 'AWS session' 'PASS' "account $($id.Account)"
        $awsOk = $true
    } catch {
        Add-Result 'AWS session' 'FAIL' "profile '$AwsProfile' not authenticated -- re-auth before the cutover"
    }
}

# --------------------------------------------------------------------- 2. CMS
Write-Host "`n2. CMS content" -ForegroundColor Cyan

$pages = @()
if ($env:LAMBDA_API_BASE_URL -and $env:MCP_API_KEY) {
    try {
        $apiBase = $env:LAMBDA_API_BASE_URL.TrimEnd('/')
        $headers = @{ 'Content-Type' = 'application/json'; 'X-API-Key' = $env:MCP_API_KEY }
        $body = @{ Name = $null; Keywords = @(); Topics = @(); Description = $null; WebsiteId = [int]$siteInfo.id } | ConvertTo-Json -Depth 5
        $r = Invoke-WebRequest -Uri "$apiBase/page/search" -Method Post -Body $body -Headers $headers -UseBasicParsing
        $pages = [System.Text.Encoding]::UTF8.GetString($r.RawContentStream.ToArray()) | ConvertFrom-Json
        Add-Result 'CMS reachable' 'PASS' "$(@($pages).Count) pages for WEBSITE_ID $($siteInfo.id)"
    } catch {
        Add-Result 'CMS reachable' 'FAIL' $_.Exception.Message
    }
} else {
    Add-Result 'CMS reachable' 'WARN' 'skipped -- env vars missing'
}

# -------------------------------------------------------------- 3. slug parity
Write-Host "`n3. Slug parity (301 target vs. rendered file location)" -ForegroundColor Cyan

if (@($pages).Count -gt 0) {
    $fnPath = Join-Path $repoRoot 'infra\cloudfront\public-clean-urls.js'
    $tmp    = Join-Path ([System.IO.Path]::GetTempPath()) "slugparity-$($siteInfo.key)"
    New-Item -ItemType Directory -Force -Path $tmp | Out-Null

    # UTF8 without BOM -- node's JSON.parse chokes on a BOM.
    $namesFile = Join-Path $tmp 'names.json'
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($namesFile, (@($pages) | ForEach-Object { $_.name } | ConvertTo-Json -Depth 3), $utf8NoBom)

    $checker = Join-Path $tmp 'check.js'
    $js = @'
const fs = require('fs');
const src = fs.readFileSync(process.argv[2], 'utf8');
const handler = new Function(src + '\nreturn handler;')();
const names = JSON.parse(fs.readFileSync(process.argv[3], 'utf8'));
function rendererSlug(n) {
  if (!n || !n.trim()) return '';
  return n.trim().toLowerCase().replace(/\s+/g, '-')
    .replace(/[^a-z0-9\-]/g, '').replace(/-{2,}/g, '-').replace(/^-+|-+$/g, '');
}
// The home page is the one name that does NOT render to /slug/index.html -- it is
// written to the bucket root, so /home/ never exists and the function 301s to /.
function rendererPath(n) {
  const s = rendererSlug(n);
  return s === 'home' ? '/' : '/' + s + '/';
}
const bad = [];
for (const n of [].concat(names)) {
  const res = handler({ request: { uri: '/', querystring: { page: { value: n } } } });
  const to = res.statusCode === 301 ? res.headers.location.value : '(no redirect)';
  const want = rendererPath(n);
  if (to !== want) bad.push(n + '  301->' + to + '  file=' + want);
}
console.log(JSON.stringify({ total: [].concat(names).length, bad: bad }));
'@
    [System.IO.File]::WriteAllText($checker, $js, $utf8NoBom)

    try {
        $out = & node $checker $fnPath $namesFile 2>&1
        $parity = $out | ConvertFrom-Json
        if ($parity.bad.Count -eq 0) {
            Add-Result 'slug parity' 'PASS' "all $($parity.total) page names agree"
        } else {
            Add-Result 'slug parity' 'FAIL' "$($parity.bad.Count) of $($parity.total) would 301 into a 404"
            foreach ($b in $parity.bad | Select-Object -First 10) { Write-Host "           $b" -ForegroundColor Red }
        }
    } catch {
        Add-Result 'slug parity' 'WARN' "could not run: $_"
    }
} else {
    Add-Result 'slug parity' 'WARN' 'skipped -- no pages loaded'
}

$slugOf = {
    param([string]$n)
    if ([string]::IsNullOrWhiteSpace($n)) { return '' }
    (($n.Trim().ToLowerInvariant() -replace '\s+', '-') -replace '[^a-z0-9\-]', '' -replace '-{2,}', '-').Trim('-')
}

# ------------------------------------------------ 3b. duplicate slugs
# Two pages whose names slug to the same string render to the SAME object key, so the
# second silently overwrites the first and one page is simply missing from the site. It
# is worst for "Home", which renders to the BUCKET ROOT: the live homepage then depends
# on whatever order the API happened to return the pages in.
if (@($pages).Count -gt 0) {
    $dupes = @($pages |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_.name) } |
        Group-Object -Property { & $slugOf $_.name } |
        Where-Object { $_.Count -gt 1 })

    if ($dupes.Count -eq 0) {
        Add-Result 'duplicate slugs' 'PASS' 'every page name maps to a distinct path'
    } else {
        # A collision on Home overwrites the site root, so it is a blocker; anything else
        # loses one interior page, which is bad but not destructive.
        $homeCollision = @($dupes | Where-Object { $_.Name -eq 'home' })
        $level = if ($homeCollision.Count -gt 0) { 'FAIL' } else { 'WARN' }
        Add-Result 'duplicate slugs' $level ("{0} slug(s) claimed by more than one page" -f $dupes.Count)
        foreach ($d in $dupes) {
            $who = (($d.Group | ForEach-Object { "id=$($_.id) '$($_.name)'" }) -join '  |  ')
            $target = if ($d.Name -eq 'home') { 'the BUCKET ROOT' } else { "/$($d.Name)/" }
            Write-Host "           $target  <-  $who" -ForegroundColor $(if ($d.Name -eq 'home') { 'Red' } else { 'Yellow' })
        }
    }
} else {
    Add-Result 'duplicate slugs' 'WARN' 'skipped -- no pages loaded'
}

# ------------------------------------------------- 3c. menu integrity
# A menu item whose pageName does not match any page renders a nav link to a URL that
# 404s -- on EVERY page of the site, since the nav is inlined into all of them. Nothing
# else in this report would notice: the pages themselves are fine.
#
# The menu lives in two places and BOTH matter:
#   s3://www-websitecontent/public/websites/{domain}/sitemenu.json   <- what the renderer
#                                                                       and the SPA read
#   the CMS database, via /menu/{websiteId}                          <- what the admin edits
# They are supposed to mirror each other. When they drift, the published nav and the nav
# you see in the admin disagree, and the next re-render silently adopts whichever the
# renderer reads.
Write-Host "`n3c. Menu integrity" -ForegroundColor Cyan

if (@($pages).Count -eq 0) {
    Add-Result 'menu links' 'WARN' 'skipped -- no pages loaded'
} else {
    $pageSlugs = @($pages | Where-Object { $_.name } | ForEach-Object { & $slugOf $_.name })

    function Test-MenuLinks {
        param($Items, [string]$Label)
        $items = @($Items)
        if ($items.Count -eq 0) { Add-Result "menu ($Label)" 'WARN' 'empty or unreadable'; return $null }
        $linked = @($items | Where-Object {
            $_.PSObject.Properties['pageName'] -and -not [string]::IsNullOrWhiteSpace($_.pageName)
        })
        $broken = @($linked | Where-Object { $pageSlugs -notcontains (& $slugOf $_.pageName) })
        $headings = $items.Count - $linked.Count
        if ($broken.Count -eq 0) {
            Add-Result "menu ($Label)" 'PASS' ("{0} items: {1} links all resolve, {2} section heading(s)" -f $items.Count, $linked.Count, $headings)
        } else {
            Add-Result "menu ($Label)" 'FAIL' ("{0} of {1} nav link(s) point at a page that does not exist" -f $broken.Count, $linked.Count)
            foreach ($b in $broken | Select-Object -First 10) {
                Write-Host ("           '{0}' -> pageName='{1}' (pageId={2})" -f $b.text, $b.pageName, $(if ($b.PSObject.Properties['pageId']) { $b.pageId } else { '-' })) -ForegroundColor Red
            }
        }
        return $items
    }

    $dbMenu = $null
    if ($env:LAMBDA_API_BASE_URL -and $env:MCP_API_KEY) {
        try {
            $mBase = $env:LAMBDA_API_BASE_URL.TrimEnd('/')
            $mResp = Invoke-WebRequest -Uri "$mBase/menu/$($siteInfo.id)" -Headers @{ 'X-API-Key' = $env:MCP_API_KEY } -Method Get -UseBasicParsing
            $dbMenu = [System.Text.Encoding]::UTF8.GetString($mResp.RawContentStream.ToArray()) | ConvertFrom-Json
            Test-MenuLinks -Items $dbMenu -Label 'database' | Out-Null
        } catch {
            Add-Result 'menu (database)' 'WARN' "could not read: $($_.Exception.Message)"
        }
    } else {
        Add-Result 'menu (database)' 'WARN' 'skipped -- env vars missing'
    }

    $s3Menu = $null
    if (-not $SkipAws -and $awsOk) {
        $menuTmp = Join-Path ([System.IO.Path]::GetTempPath()) "sitemenu-$($siteInfo.key).json"
        $cp = Invoke-Native -Exe 'aws' -Arguments @('s3', 'cp',
            "s3://www-websitecontent/public/websites/$domain/sitemenu.json", $menuTmp,
            '--profile', $AwsProfile, '--region', $Region, '--quiet')
        if ($cp.ExitCode -eq 0 -and (Test-Path $menuTmp)) {
            try {
                $s3Menu = [System.IO.File]::ReadAllText($menuTmp) | ConvertFrom-Json
                Test-MenuLinks -Items $s3Menu -Label 'S3 sitemenu.json' | Out-Null
            } catch {
                Add-Result 'menu (S3 sitemenu.json)' 'WARN' "unreadable: $($_.Exception.Message)"
            }
        } else {
            Add-Result 'menu (S3 sitemenu.json)' 'WARN' 'not found in the content bucket'
        }
    }

    # Drift between the two stores. Compared on the link fields only -- display text can
    # legitimately differ in encoding between the JSON file and the API response.
    if ($null -ne $dbMenu -and $null -ne $s3Menu) {
        $keyOf = { param($i) "{0}|{1}|{2}" -f $i.id, $(if ($i.PSObject.Properties['pageName']) { $i.pageName } else { '' }), $(if ($i.PSObject.Properties['parent']) { $i.parent } else { '' }) }
        $dbKeys = @($dbMenu | ForEach-Object { & $keyOf $_ }) | Sort-Object
        $s3Keys = @($s3Menu | ForEach-Object { & $keyOf $_ }) | Sort-Object
        if (@($dbMenu).Count -ne @($s3Menu).Count) {
            Add-Result 'menu stores agree' 'WARN' ("different item counts -- DB {0}, S3 {1}" -f @($dbMenu).Count, @($s3Menu).Count)
        } elseif (($dbKeys -join "`n") -ne ($s3Keys -join "`n")) {
            Add-Result 'menu stores agree' 'WARN' 'the database and sitemenu.json disagree on links or nesting'
        } else {
            Add-Result 'menu stores agree' 'PASS' 'database and sitemenu.json match'
        }
    }
}

# ------------------------------------------------------- 4. function unit tests
Write-Host "`n4. CloudFront function unit tests" -ForegroundColor Cyan

$testFile = Join-Path $repoRoot 'infra\cloudfront\public-clean-urls.test.js'
if (Test-Path $testFile) {
    $out = & node $testFile 2>&1
    $exit = $LASTEXITCODE
    $summary = @($out | Where-Object { $_ -match 'passed' })
    $line = if ($summary.Count -gt 0) { ($summary[0] -replace '^\s+', '') } else { 'no summary line' }
    if ($exit -eq 0) { Add-Result 'function tests' 'PASS' $line }
    else { Add-Result 'function tests' 'FAIL' $line }
} else {
    Add-Result 'function tests' 'WARN' 'test file not found'
}

if ($SkipAws -or -not $awsOk) {
    Write-Host "`n(AWS checks skipped)" -ForegroundColor DarkGray
} else {

# ------------------------------------------------------------ 5. public bucket
Write-Host "`n5. Public bucket ($pubBucket)" -ForegroundColor Cyan

if (Test-AwsResource @('s3api', 'head-bucket', '--bucket', $pubBucket, '--profile', $AwsProfile, '--region', $Region)) {
    Add-Result 'bucket exists' 'PASS' "s3://$pubBucket"

    try {
        $web = Invoke-Aws @('s3api', 'get-bucket-website', '--bucket', $pubBucket, '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
        Add-Result 'website hosting' 'PASS' "index=$($web.IndexDocument.Suffix)"
        # Whether clean URLs actually resolve depends on which ENDPOINT CloudFront calls,
        # not on this setting -- see the origin check in section 6. Claiming "clean URLs
        # work natively" here was wrong for REST-origin distributions and sent a site into
        # `verify` that could only ever 403.
    } catch {
        Add-Result 'website hosting' 'WARN' 'not enabled -- /slug/ will need the CloudFront function'
    }

    try {
        $root = Invoke-Aws @('s3api', 'head-object', '--bucket', $pubBucket, '--key', 'index.html', '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
        $cc = if ($root.PSObject.Properties['CacheControl']) { $root.CacheControl } else { '(none -- CloudFront default TTL, up to 24h)' }
        Add-Result 'root index.html' 'PASS' "$([math]::Round($root.ContentLength/1KB,1)) KB, modified $($root.LastModified)"
        Add-Result 'root cache-control' $(if ($root.PSObject.Properties['CacheControl']) { 'PASS' } else { 'WARN' }) $cc
    } catch {
        Add-Result 'root index.html' 'WARN' 'not found'
    }
} else {
    Add-Result 'bucket exists' 'FAIL' "s3://$pubBucket not found or no access"
}

# ------------------------------------------------------ 6. public distribution
Write-Host "`n6. Public CloudFront distribution" -ForegroundColor Cyan

$dist = Get-DistributionByAlias -Alias $publicHost -AwsProfile $AwsProfile
if (-not $dist) { $dist = Get-DistributionByAlias -Alias $domain -AwsProfile $AwsProfile }

if ($dist) {
    Add-Result 'distribution' 'PASS' "$($dist.Id)  ($($dist.DomainName))"

    # WHICH S3 ENDPOINT the origin points at decides whether /slug/ resolves at all.
    #   *.s3-website-{region}.amazonaws.com  -- resolves /slug/ -> /slug/index.html, 404s properly
    #   *.s3.amazonaws.com                   -- REST: /slug/ is a missing key, and S3 answers
    #                                           403 to callers without ListBucket
    # A REST origin makes every clean URL 403 until the CloudFront function is attached,
    # which means `-Phase verify` cannot pass before `-Phase routing` has already made a
    # visible change -- losing the safety property the whole phased cutover is built on.
    $originDomain = $null
    $originItems = @(Get-Prop $dist 'Origins.Items')
    if ($originItems.Count -gt 0) { $originDomain = Get-Prop $originItems[0] 'DomainName' }

    if (-not $originDomain) {
        Add-Result 'origin endpoint' 'WARN' 'could not read the origin'
    } elseif ($originDomain -like '*s3-website*') {
        Add-Result 'origin endpoint' 'PASS' "$originDomain (website -- /slug/ resolves natively)"
    } elseif ($originDomain -like '*.s3.amazonaws.com' -or $originDomain -like '*.s3.*.amazonaws.com') {
        Add-Result 'origin endpoint' 'FAIL' "$originDomain (REST -- every /slug/ will 403). Fix: ./scripts/set-public-origin.ps1 -Site $($siteInfo.key)"
    } else {
        Add-Result 'origin endpoint' 'WARN' "$originDomain (not an S3 endpoint this script recognises)"
    }

    $assoc = $dist.DefaultCacheBehavior.FunctionAssociations
    if ($assoc.Quantity -gt 0) {
        foreach ($a in $assoc.Items) { Add-Result 'function attached' 'INFO' "$($a.EventType): $($a.FunctionARN)" }
    } else {
        Add-Result 'function attached' 'INFO' 'none -- legacy ?page= links still served by the SPA'
    }

    $errs = $dist.CustomErrorResponses
    if ($errs.Quantity -gt 0) {
        foreach ($e in $errs.Items) {
            Add-Result 'error response' 'INFO' "$($e.ErrorCode) -> $($e.ResponsePagePath) as $($e.ResponseCode)"
        }
    } else {
        Add-Result 'error response' 'INFO' 'none configured'
    }
} else {
    Add-Result 'distribution' 'FAIL' "no distribution with alias $publicHost or $domain"
}

# --------------------------------------------------- 7. static page coverage
Write-Host "`n7. Static page coverage in the public bucket" -ForegroundColor Cyan

if (@($pages).Count -gt 0) {
    try {
        $keysRaw = (Invoke-Native -Exe 'aws' -Arguments @('s3api', 'list-objects-v2', '--bucket', $pubBucket,
            '--profile', $AwsProfile, '--region', $Region,
            '--query', 'Contents[?ends_with(Key, `index.html`)].Key', '--output', 'json')).Output -join "`n"
        # Parse into a variable FIRST: in Windows PowerShell 5.1, ConvertFrom-Json emits
        # a JSON array to the pipeline as ONE object rather than enumerating it, so
        # @($raw | ConvertFrom-Json) yields a single element that IS the array -- and
        # every key then looks absent. Also array-wrap, because the JMESPath query
        # returns null when nothing matches and a bare string when exactly one does.
        $keysParsed = $keysRaw | ConvertFrom-Json
        $keys = @($keysParsed)
        $keySet = @{}
        foreach ($k in $keys) { if ($k) { $keySet[$k] = $true } }

        # Home is counted SEPARATELY. It lives at the bucket root index.html, which
        # already exists -- it is the live SPA shell. Mere existence therefore proves
        # nothing, and counting it as "covered" overstates readiness. Distinguish by
        # size: the SPA shell is ~1 KB, a pre-rendered page is tens of KB.
        # The @() must wrap the PIPELINE RESULT, not the input: a one-page site
        # (reflectiverealizations) returns a scalar otherwise, and .Count blows up.
        $served = @(@($pages) | Where-Object { $_.name -and $_.name.Trim() -and $_.name -ine 'Home' })
        $missing = @()
        foreach ($p in $served) {
            $slug = ($p.name.Trim().ToLowerInvariant() -replace '\s+', '-') -replace '[^a-z0-9\-]', ''
            $slug = ($slug -replace '-{2,}', '-').Trim('-')
            if (-not $keySet.ContainsKey("$slug/index.html")) { $missing += "$slug/index.html" }
        }

        $have = $served.Count - $missing.Count
        if ($missing.Count -eq 0) {
            Add-Result 'page coverage' 'PASS' "$have/$($served.Count) non-Home pages rendered -- safe to attach routing"
        } else {
            $status = if ($have -eq 0) { 'INFO' } else { 'WARN' }
            $note = if ($have -eq 0) { 'backfill not run yet (expected at this stage)' } else { 'DO NOT attach routing until this is 0' }
            Add-Result 'page coverage' $status "$have/$($served.Count) present, $($missing.Count) missing -- $note"
            foreach ($m in $missing | Select-Object -First 10) { Write-Host "           missing: $m" -ForegroundColor Yellow }
            if ($missing.Count -gt 10) { Write-Host "           ... and $($missing.Count - 10) more" -ForegroundColor Yellow }
        }

        # Home / bucket root, reported on its own terms.
        try {
            $rootObj = Invoke-Aws @('s3api', 'head-object', '--bucket', $pubBucket, '--key', 'index.html',
                '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
            $kb = [math]::Round($rootObj.ContentLength / 1KB, 1)
            if ($rootObj.ContentLength -lt 4096) {
                Add-Result 'home page (root)' 'INFO' "$kb KB -- still the SPA shell; the 'root' phase replaces it"
            } else {
                Add-Result 'home page (root)' 'PASS' "$kb KB -- looks pre-rendered"
            }
        } catch {
            Add-Result 'home page (root)' 'WARN' 'no index.html at the bucket root'
        }
    } catch {
        Add-Result 'page coverage' 'WARN' "could not list bucket: $_"
    }
} else {
    Add-Result 'page coverage' 'WARN' 'skipped -- no pages loaded'
}

# ------------------------------------------------------- 8. admin subdomain
Write-Host "`n8. Admin subdomain ($adminHost)" -ForegroundColor Cyan

$adminBucket = Get-AdminBucketName -SiteKey $siteInfo.key
if (Test-AwsResource @('s3api', 'head-bucket', '--bucket', $adminBucket, '--profile', $AwsProfile, '--region', $Region)) { Add-Result 'admin bucket' 'PASS' "s3://$adminBucket" }
else { Add-Result 'admin bucket' 'INFO' 'not created yet -- deploy-admin-subdomain.ps1' }

try {
    $certs = Invoke-Aws @('acm', 'list-certificates', '--region', 'us-east-1', '--profile', $AwsProfile,
        '--certificate-statuses', 'ISSUED', 'PENDING_VALIDATION', '--output', 'json')
    $c = $certs.CertificateSummaryList | Where-Object { $_.DomainName -eq $adminHost -or $_.DomainName -eq "*.$domain" } | Select-Object -First 1
    if ($c) {
        $d = Invoke-Aws @('acm', 'describe-certificate', '--certificate-arn', $c.CertificateArn, '--region', 'us-east-1', '--profile', $AwsProfile, '--output', 'json')
        Add-Result 'admin certificate' $(if ($d.Certificate.Status -eq 'ISSUED') { 'PASS' } else { 'WARN' }) "$($c.DomainName): $($d.Certificate.Status)"
    } else {
        Add-Result 'admin certificate' 'INFO' 'none yet'
    }
} catch { Add-Result 'admin certificate' 'WARN' $_.Exception.Message }

$adminDist = Get-DistributionByAlias -Alias $adminHost -AwsProfile $AwsProfile
if ($adminDist) { Add-Result 'admin distribution' 'PASS' $adminDist.Id }
else { Add-Result 'admin distribution' 'INFO' 'not created yet' }

try {
    $r = Resolve-DnsName -Name $adminHost -Type CNAME -Server 8.8.8.8 -DnsOnly -ErrorAction Stop
    Add-Result 'admin DNS' 'PASS' "-> $($r[0].NameHost)"
} catch { Add-Result 'admin DNS' 'INFO' 'does not resolve yet' }

}  # end AWS checks

# ----------------------------------------------------------------- verdict
Write-Host "`n$('=' * 78)"
$fails = @($results | Where-Object { $_.Status -eq 'FAIL' })
$warns = @($results | Where-Object { $_.Status -eq 'WARN' })

if ($fails.Count -gt 0) {
    Write-Host "UNHEALTHY -- $($fails.Count) problem(s) that affect the live site:" -ForegroundColor Red
    foreach ($f in $fails) { Write-Host "  * $($f.Check): $($f.Detail)" -ForegroundColor Red }
} elseif ($warns.Count -gt 0) {
    Write-Host "HEALTHY, with $($warns.Count) thing(s) worth a look:" -ForegroundColor Yellow
    foreach ($w in $warns) { Write-Host "  * $($w.Check): $($w.Detail)" -ForegroundColor Yellow }
} else {
    Write-Host 'HEALTHY -- all checks passed.' -ForegroundColor Green
}
Write-Host ''
