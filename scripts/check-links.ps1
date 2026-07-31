<#
.SYNOPSIS
    Find broken links on one site, several, or all of them.

.DESCRIPTION
    Reads a site's sitemap.xml, fetches every page, extracts every href, and reports the
    FINAL status after following redirects. Following matters: a 301 that lands on a 404 is
    a broken link, and checking the redirect alone would call it healthy.

    Each distinct URL is checked ONCE however many pages reference it, so a scripture link
    used on 200 pages costs one request. That is what makes a 471-page site tractable.

    Reports every non-200 with the referring pages, so a hit is immediately actionable.

    READ-ONLY. Makes no changes to anything.

.PARAMETER Site
    Site key(s) from sites.json -- e.g. cesletter, ldsapologetics. Accepts several.

.PARAMETER Domain
    Check a bare domain instead, for something not in sites.json (e.g. ldsgospeldoctrine.info).

.PARAMETER All
    Every site in sites.json.

.PARAMETER InternalOnly
    Only check links pointing at the site being crawled. Much faster, and covers the links
    you actually control. Use for a quick regression check after a content change.

.PARAMETER ExcludeHost
    Hostnames to skip, e.g. churchofjesuschrist.org. Repeatable. Useful when an external
    host rate-limits and floods the report with false failures.

.PARAMETER ThrottleMs
    Pause between requests, in milliseconds. Default 0. Raise it if an external host starts
    returning 429.

.PARAMETER ReportPath
    Write a CSV of the findings here as well as printing them.

.EXAMPLE
    ./scripts/check-links.ps1 -Site cesletter

.EXAMPLE
    ./scripts/check-links.ps1 -All -InternalOnly

.EXAMPLE
    ./scripts/check-links.ps1 -All -ExcludeHost churchofjesuschrist.org -ReportPath .\links.csv

.NOTES
    Exit code is 1 when anything broken is found, 0 when clean -- so it can gate a release.
#>

[CmdletBinding()]
param(
    [string[]]$Site,
    [string[]]$Domain,
    [switch]$All,
    [switch]$InternalOnly,
    [string[]]$ExcludeHost = @(),
    [int]$ThrottleMs = 0,
    [string]$ReportPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# Windows PowerShell 5.1 negotiates TLS 1.0 against some hosts by default.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
[Net.ServicePointManager]::DefaultConnectionLimit = 16

# A browser UA, deliberately. A Googlebot UA coming from a non-Google IP is exactly what
# bot protection blocks -- Wikipedia answered it with 403 while giving a browser UA a 429.
# Neither means the link is broken, and pretending to be Googlebot only adds noise.
$UA = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0 Safari/537.36'

# ---------------------------------------------------------------- resolve targets
$targets = New-Object System.Collections.ArrayList

if ($All -or $Site) {
    $registryPath = Join-Path $PSScriptRoot 'sites.json'
    $registry = Get-Content $registryPath -Raw | ConvertFrom-Json
    foreach ($s in $registry.sites) {
        if ($All -or ($Site -contains $s.key)) { [void]$targets.Add($s.domain) }
    }
    if ($Site) {
        foreach ($k in $Site) {
            if (-not ($registry.sites | Where-Object { $_.key -eq $k })) {
                Write-Warning "site '$k' is not in sites.json -- use -Domain for it"
            }
        }
    }
}
foreach ($d in $Domain) { [void]$targets.Add($d) }

if ($targets.Count -eq 0) { throw 'Nothing to check. Pass -Site, -Domain or -All.' }

# ---------------------------------------------------------------- helpers
function Get-FinalStatus {
    <#
        Follow redirects BY HAND and return the final status code.

        Invoke-WebRequest on Windows PowerShell 5.1 does not follow 308 Permanent Redirect
        even with -MaximumRedirection set, so it reports 308 as the final status. That made
        14 perfectly healthy interpreterfoundation.org links look broken on the first run.
        HttpWebRequest with AllowAutoRedirect=$false plus an explicit loop handles every 3xx
        uniformly, including 307 and 308.

        Returns 0 when the host could not be reached at all (DNS/TLS/connection refused),
        and -1 when the redirect chain never terminates.
    #>
    param([string]$Url, [int]$MaxHops = 6)

    $current = $Url
    for ($hop = 0; $hop -lt $MaxHops; $hop++) {
        $code = 0
        $loc = $null
        try {
            $req = [System.Net.HttpWebRequest]::Create($current)
            $req.UserAgent = $UA
            $req.AllowAutoRedirect = $false
            $req.Timeout = 25000
            $req.Method = 'GET'
            $resp = $req.GetResponse()
            $code = [int]$resp.StatusCode
            $loc = $resp.Headers['Location']
            $resp.Close()
        } catch [System.Net.WebException] {
            $resp = $null
            try { $resp = $_.Exception.Response } catch { }
            if (-not $resp) { return 0 }
            $code = [int]$resp.StatusCode
            try { $loc = $resp.Headers['Location'] } catch { $loc = $null }
            try { $resp.Close() } catch { }
        } catch {
            return 0
        }

        if ($code -ge 300 -and $code -lt 400 -and $loc) {
            try { $current = [uri]::new([uri]$current, $loc).AbsoluteUri } catch { return $code }
            continue
        }
        return $code
    }
    return -1
}

# 401/403/429 mean a bot wall, not a dead link -- the page is there, the checker just is not
# allowed through. Wikipedia, JSTOR and BYU RSC all do this. Reported separately so the
# BROKEN list stays actionable.
#
# Any 2xx is fine. An earlier version only accepted 200 and duly reported a 202 as broken.
function Get-StatusClass {
    param([int]$Code)
    if ($Code -ge 200 -and $Code -lt 300) { return 'ok' }
    if ($Code -in 401, 403, 429) { return 'blocked' }
    return 'broken'
}

# Is this the kind of failure that a busy host produces under load rather than a dead URL?
# Crawling 2,889 links hits a handful of hosts hundreds of times: churchofjesuschrist.org
# answered 71 requests with 503 purely because of the rate, and every one of those URLs
# resolves fine on its own. Anything here gets re-checked slowly before being called broken.
function Test-TransientStatus {
    param([int]$Code)
    return ($Code -eq 0 -or $Code -eq 429 -or $Code -ge 500)
}

$allFindings = New-Object System.Collections.ArrayList
$grandBroken = 0

foreach ($dom in $targets) {
    Write-Host ""
    Write-Host "===== $dom =====" -ForegroundColor White

    try {
        $sm = (Invoke-WebRequest -Uri "https://www.$dom/sitemap.xml" -UseBasicParsing -TimeoutSec 30 -UserAgent $UA).Content
    } catch {
        Write-Host "  sitemap fetch FAILED -- skipping" -ForegroundColor Red
        continue
    }
    $pages = @([regex]::Matches($sm, '<loc>(.*?)</loc>') | ForEach-Object { $_.Groups[1].Value.Trim() })
    Write-Host "  pages in sitemap : $($pages.Count)"

    # url -> referring pages
    $links = @{}
    $fetchFailures = 0
    foreach ($p in $pages) {
        try { $html = (Invoke-WebRequest -Uri $p -UseBasicParsing -TimeoutSec 30 -UserAgent $UA).Content }
        catch { $fetchFailures++; Write-Host "    page fetch FAILED: $p" -ForegroundColor Red; continue }

        foreach ($m in [regex]::Matches($html, 'href="(https?://[^"]+)"')) {
            $u = $m.Groups[1].Value
            $u = $u -replace '&amp;', '&'          # hrefs are HTML-escaped in the source
            $uHost = ''
            try { $uHost = ([uri]$u).Host } catch { continue }

            if ($InternalOnly -and $uHost -ne "www.$dom" -and $uHost -ne $dom) { continue }
            if ($ExcludeHost | Where-Object { $uHost -like "*$_" }) { continue }

            if (-not $links.ContainsKey($u)) { $links[$u] = New-Object System.Collections.ArrayList }
            [void]$links[$u].Add($p)
        }
        if ($ThrottleMs -gt 0) { Start-Sleep -Milliseconds $ThrottleMs }
    }

    Write-Host "  distinct links   : $($links.Keys.Count)"
    if ($fetchFailures -gt 0) { Write-Host "  page fetch fails : $fetchFailures" -ForegroundColor Yellow }

    $broken  = New-Object System.Collections.ArrayList
    $blocked = New-Object System.Collections.ArrayList
    $suspect = New-Object System.Collections.ArrayList   # failed pass 1, needs a slow re-check
    $checked = 0
    foreach ($u in ($links.Keys | Sort-Object)) {
        $checked++
        if ($checked % 200 -eq 0) { Write-Host "    ...checked $checked / $($links.Keys.Count)" -ForegroundColor DarkGray }
        $code = Get-FinalStatus -Url $u
        $class = Get-StatusClass -Code $code
        if ($class -eq 'ok') { if ($ThrottleMs -gt 0) { Start-Sleep -Milliseconds $ThrottleMs }; continue }

        if (Test-TransientStatus -Code $code) {
            [void]$suspect.Add($u)
        } else {
            $finding = [pscustomobject]@{
                Site = $dom; Class = $class; Status = $code; Url = $u
                RefCount = $links[$u].Count; FirstRef = $links[$u][0]
            }
            if ($class -eq 'broken') { [void]$broken.Add($finding) } else { [void]$blocked.Add($finding) }
            [void]$allFindings.Add($finding)
        }
        if ($ThrottleMs -gt 0) { Start-Sleep -Milliseconds $ThrottleMs }
    }

    # PASS 2 -- the whole point of this script being trustworthy. Re-check every transient
    # failure one at a time with a real pause. Only a second failure counts.
    if ($suspect.Count -gt 0) {
        Write-Host "    re-checking $($suspect.Count) transient failure(s) slowly..." -ForegroundColor DarkGray
        Start-Sleep -Seconds 20
        foreach ($u in $suspect) {
            $code = Get-FinalStatus -Url $u
            $class = Get-StatusClass -Code $code
            if ($class -eq 'ok') { Start-Sleep -Milliseconds 750; continue }
            $finding = [pscustomobject]@{
                Site = $dom; Class = $class; Status = $code; Url = $u
                RefCount = $links[$u].Count; FirstRef = $links[$u][0]
            }
            if ($class -eq 'broken') { [void]$broken.Add($finding) } else { [void]$blocked.Add($finding) }
            [void]$allFindings.Add($finding)
            Start-Sleep -Milliseconds 750
        }
    }

    if ($broken.Count -eq 0) {
        Write-Host "  BROKEN: none" -ForegroundColor Green
    } else {
        $grandBroken += $broken.Count
        Write-Host "  BROKEN: $($broken.Count)" -ForegroundColor Yellow
        $broken | Sort-Object Status, Url | ForEach-Object {
            Write-Host ("    {0,-4} refs={1,-4} {2}" -f $_.Status, $_.RefCount, $_.Url) -ForegroundColor Yellow
            Write-Host ("           first seen on: {0}" -f $_.FirstRef) -ForegroundColor DarkGray
        }
    }
    if ($blocked.Count -gt 0) {
        Write-Host "  blocked by bot protection (NOT broken): $($blocked.Count)" -ForegroundColor DarkGray
        $blocked | Sort-Object Url | ForEach-Object {
            Write-Host ("    {0,-4} {1}" -f $_.Status, $_.Url) -ForegroundColor DarkGray
        }
    }
}

Write-Host ""
Write-Host "================ SUMMARY ================" -ForegroundColor White
if ($grandBroken -eq 0) {
    Write-Host "No broken links across $($targets.Count) site(s)." -ForegroundColor Green
} else {
    Write-Host "$grandBroken broken link(s) across $($targets.Count) site(s)." -ForegroundColor Yellow
    $allFindings | Group-Object Site | ForEach-Object { "  {0,-28} {1}" -f $_.Name, $_.Count }
}

if ($ReportPath) {
    $allFindings | Export-Csv -Path $ReportPath -NoTypeInformation -Encoding UTF8
    Write-Host "`nreport written: $ReportPath"
}

if ($grandBroken -gt 0) { exit 1 } else { exit 0 }
