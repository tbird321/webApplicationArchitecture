<#
.SYNOPSIS
    DNS helpers for domains whose authoritative nameservers are GoDaddy's.

.DESCRIPTION
    All six domains are registered at GoDaddy and resolved by GoDaddy's
    nameservers -- there is no Route 53 hosted zone. That is fine for everything
    the admin subdomain needs, because admin.{domain} is a SUBDOMAIN: a plain
    CNAME to the CloudFront distribution works. (The apex is the case that needs
    an ALIAS/ANAME record, which GoDaddy does not offer for CloudFront -- but the
    apex is not involved here.)

    Two modes:

    * API mode -- if credentials are present, records are created for you through
      GoDaddy's REST API. See CREDENTIALS below.

    * Manual mode -- the exact record to create is printed, and the script then
      polls public DNS until it sees the value, so you can paste it into the
      GoDaddy DNS panel and the script picks up automatically.

    CREDENTIALS
    -----------
    GoDaddy has two credential types. This script accepts either, and PREFERS the
    Personal Access Token when both are present.

    1. Personal Access Token (PAT) -- RECOMMENDED.
           Authorization: Bearer <token>
       Works with v1, v2 and v3 endpoints. Scoped, individually revocable, and can
       be given an expiry. This script only calls v1 DNS record endpoints, so the
       token needs the "domains.dns:update" scope.

           [Environment]::SetEnvironmentVariable('GODADDY_PAT','...','User')

    2. Classic developer key + secret -- LEGACY, being retired.
           Authorization: sso-key <key>:<secret>
       Still accepted on v1 endpoints (which is all this script uses), but GoDaddy
       has it scheduled for discontinuation after 2026, and it does not work with
       v3 at all. Kept here so an existing key kceps working, but prefer the PAT.

           [Environment]::SetEnvironmentVariable('GODADDY_API_KEY','...','User')
           [Environment]::SetEnvironmentVariable('GODADDY_API_SECRET','...','User')

    Both are created at https://developer.godaddy.com/keys -- generate a PRODUCTION
    credential, not an OTE/test one. GoDaddy restricts API access on some account
    tiers; a 403 ACCESS_DENIED means this account cannot use the API, and the
    script falls back to manual mode automatically.
#>

Set-StrictMode -Version Latest

# Windows PowerShell 5.1 still negotiates TLS 1.0 by default against some hosts.
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$script:GODADDY_API = 'https://api.godaddy.com/v1'
# GoDaddy rejects TTLs below 600.
$script:GODADDY_MIN_TTL = 600

function Test-GoDaddyApi {
    <#
    .SYNOPSIS
        True if usable API credentials are present -- either a PAT or a key+secret.
    #>
    if ($env:GODADDY_PAT) { return $true }
    return [bool]($env:GODADDY_API_KEY -and $env:GODADDY_API_SECRET)
}

function Get-GoDaddyAuthKind {
    <#
    .SYNOPSIS
        Which credential is in play: 'pat', 'sso-key', or 'none'. For reporting.
    #>
    if ($env:GODADDY_PAT) { return 'pat' }
    if ($env:GODADDY_API_KEY -and $env:GODADDY_API_SECRET) { return 'sso-key' }
    return 'none'
}

function Get-GoDaddyHeaders {
    <#
    .SYNOPSIS
        Build the Authorization header, preferring the PAT.

    .DESCRIPTION
        PAT  -> "Bearer <token>"          (current; works on v1/v2/v3)
        key  -> "sso-key <key>:<secret>"  (legacy; v1 only, retired after 2026)

        This script only calls v1 DNS record endpoints, so either works today.
    #>
    switch (Get-GoDaddyAuthKind) {
        'pat' {
            return @{
                'Authorization' = "Bearer $($env:GODADDY_PAT)"
                'Content-Type'  = 'application/json'
            }
        }
        'sso-key' {
            return @{
                'Authorization' = "sso-key $($env:GODADDY_API_KEY):$($env:GODADDY_API_SECRET)"
                'Content-Type'  = 'application/json'
            }
        }
        default {
            throw 'No GoDaddy credentials found. Set GODADDY_PAT (preferred), or GODADDY_API_KEY + GODADDY_API_SECRET.'
        }
    }
}

function Test-GoDaddyAccess {
    <#
    .SYNOPSIS
        Read-only check that the credential actually works for a domain. Changes nothing.

    .DESCRIPTION
        Verifying this in daylight is the whole point: a PAT with the wrong scope, an
        OTE token, or an account tier without API access all fail identically at 2am
        when you are halfway through a cutover.

        Does a GET on the domain's records. Returns an object:
            Ok       $true if the credential can read the domain
            Kind     'pat' | 'sso-key' | 'none'
            Detail   human-readable result
            CanWrite $null unless we can infer it (a read failure does not prove a
                     write failure -- the scopes are separate)
    #>
    param([Parameter(Mandatory)][string]$Domain)

    $kind = Get-GoDaddyAuthKind
    if ($kind -eq 'none') {
        return [pscustomobject]@{ Ok = $false; Kind = $kind; CanWrite = $false
            Detail = 'no credentials set (GODADDY_PAT preferred)' }
    }

    try {
        $uri = "$script:GODADDY_API/domains/$Domain/records"
        $r = Invoke-RestMethod -Method Get -Uri $uri -Headers (Get-GoDaddyHeaders)
        $count = @($r).Count
        return [pscustomobject]@{ Ok = $true; Kind = $kind; CanWrite = $null
            Detail = "$kind credential can read $Domain ($count records)" }
    }
    catch {
        $status = $null
        if ($_.Exception.PSObject.Properties['Response'] -and $_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        $detail = switch ($status) {
            401 { "401 Unauthorized -- $kind credential is wrong, expired, or an OTE/test one" }
            403 { "403 Access denied -- token likely missing a DNS scope, or the account tier has no API access" }
            404 { "404 -- $Domain is not in this GoDaddy account" }
            default { "failed ($status): $($_.Exception.Message)" }
        }
        return [pscustomobject]@{ Ok = $false; Kind = $kind; CanWrite = $null; Detail = $detail }
    }
}

function ConvertTo-RelativeName {
    <#
    .SYNOPSIS
        Turn a fully-qualified record name into the relative name GoDaddy wants.

    .EXAMPLE
        ConvertTo-RelativeName '_x1.admin.example.com.' 'example.com'  ->  '_x1.admin'
        ConvertTo-RelativeName 'admin.example.com'      'example.com'  ->  'admin'
        ConvertTo-RelativeName 'example.com'            'example.com'  ->  '@'
    #>
    param(
        [Parameter(Mandatory)][string]$FullName,
        [Parameter(Mandatory)][string]$Domain
    )
    $n = $FullName.TrimEnd('.')
    if ($n -eq $Domain) { return '@' }
    if ($n.EndsWith(".$Domain")) { return $n.Substring(0, $n.Length - $Domain.Length - 1) }
    return $n
}

function Set-GoDaddyRecord {
    <#
    .SYNOPSIS
        Create or replace a DNS record at GoDaddy via the API.

    .DESCRIPTION
        Uses PUT /domains/{domain}/records/{type}/{name}, which REPLACES all
        records of that type at that name. That is what we want for both the ACM
        validation CNAME and the admin CNAME -- there should only ever be one.
    #>
    param(
        [Parameter(Mandatory)][string]$Domain,
        [Parameter(Mandatory)][string]$Name,      # relative, e.g. 'admin'
        [Parameter(Mandatory)][ValidateSet('A', 'AAAA', 'CNAME', 'TXT', 'MX')][string]$Type,
        [Parameter(Mandatory)][string]$Value,
        [int]$Ttl = 600
    )

    if ($Ttl -lt $script:GODADDY_MIN_TTL) { $Ttl = $script:GODADDY_MIN_TTL }
    $uri  = "$script:GODADDY_API/domains/$Domain/records/$Type/$([uri]::EscapeDataString($Name))"
    $body = ConvertTo-Json @(@{ data = $Value.TrimEnd('.'); ttl = $Ttl }) -Depth 5

    try {
        Invoke-RestMethod -Method Put -Uri $uri -Headers (Get-GoDaddyHeaders) -Body $body | Out-Null
        Write-Ok "GoDaddy: $Type $Name.$Domain -> $Value"
        return $true
    } catch {
        $status = $null
        if ($_.Exception.PSObject.Properties['Response'] -and $_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }
        $kind = Get-GoDaddyAuthKind
        switch ($status) {
            401 {
                Write-Warn "GoDaddy returned 401 Unauthorized (credential type: $kind)."
                if ($kind -eq 'pat') {
                    Write-Warn '  The PAT is wrong, expired, or is an OTE/test token rather than a production one.'
                } else {
                    Write-Warn '  The key/secret is wrong, or is an OTE/test pair rather than a production one.'
                }
            }
            403 {
                Write-Warn "GoDaddy returned 403 ACCESS_DENIED (credential type: $kind)."
                if ($kind -eq 'pat') {
                    Write-Warn '  Most likely the token is missing the "domains.dns:update" scope.'
                } else {
                    Write-Warn '  Classic keys are being retired; a PAT with "domains.dns:update" is the supported path.'
                }
                Write-Warn '  It can also mean this account tier is not eligible for API access at all.'
            }
            404 { Write-Warn "GoDaddy returned 404 for $Domain -- is the domain in this account?" }
            422 { Write-Warn "GoDaddy returned 422 -- the record value was rejected: $Value" }
            default { Write-Warn "GoDaddy API call failed ($status): $($_.Exception.Message)" }
        }
        Write-Warn '  Falling back to manual DNS entry.'
        return $false
    }
}

function Show-ManualDnsRecord {
    <#
    .SYNOPSIS
        Print copy-pasteable instructions for the GoDaddy DNS panel.
    #>
    param(
        [Parameter(Mandatory)][string]$Domain,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Type,
        [Parameter(Mandatory)][string]$Value,
        [string]$Purpose = ''
    )
    Write-Host ''
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor Yellow
    Write-Host '   ADD THIS RECORD AT GODADDY' -ForegroundColor Yellow
    if ($Purpose) { Write-Host "   ($Purpose)" -ForegroundColor DarkGray }
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor Yellow
    Write-Host "   godaddy.com -> My Products -> $Domain -> DNS -> Add New Record"
    Write-Host ''
    Write-Host "     Type  : $Type"
    Write-Host "     Name  : $Name"
    Write-Host "     Value : $Value"
    Write-Host "     TTL   : 600 seconds (or 1 hour)"
    Write-Host ''
    Write-Host '  ---------------------------------------------------------------' -ForegroundColor Yellow
}

function Wait-DnsRecord {
    <#
    .SYNOPSIS
        Poll public DNS until a record resolves to the expected value.

    .DESCRIPTION
        Queries a public resolver (8.8.8.8) rather than the local one, so a stale
        local cache or a split-horizon resolver does not report a false negative.
        Returns $true once seen, $false on timeout.
    #>
    param(
        [Parameter(Mandatory)][string]$FullName,
        [Parameter(Mandatory)][ValidateSet('A', 'AAAA', 'CNAME', 'TXT')][string]$Type,
        [string]$ExpectedValue,
        [int]$TimeoutMinutes = 20,
        # Several public resolvers, not one. Propagation is uneven -- Cloudflare
        # routinely has a new GoDaddy record a minute or two before Google does --
        # and ACM uses its own resolvers anyway, so it only has to be visible
        # SOMEWHERE. Waiting on one specific resolver just adds dead time.
        [string[]]$Resolvers = @('1.1.1.1', '8.8.8.8', '9.9.9.9')
    )

    $name = $FullName.TrimEnd('.')
    $want = if ($ExpectedValue) { $ExpectedValue.TrimEnd('.') } else { $null }
    Write-Note "waiting for DNS: $Type $name"
    Write-Note "  resolvers: $($Resolvers -join ', ')  (up to ${TimeoutMinutes}m)"

    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    $round = 0

    while ((Get-Date) -lt $deadline) {
        $round++
        foreach ($resolver in $Resolvers) {
            try {
                $answers = Resolve-DnsName -Name $name -Type $Type -Server $resolver -DnsOnly -ErrorAction Stop
                foreach ($a in $answers) {
                    $got = $null
                    if ($a.PSObject.Properties['NameHost'])       { $got = $a.NameHost }
                    elseif ($a.PSObject.Properties['Text'])       { $got = ($a.Text -join '') }
                    elseif ($a.PSObject.Properties['IPAddress'])  { $got = $a.IPAddress }
                    if (-not $got) { continue }
                    if (-not $want -or $got.TrimEnd('.') -eq $want) {
                        Write-Ok "$name -> $got  (seen by $resolver)"
                        return $true
                    }
                }
            } catch {
                # NXDOMAIN while the record propagates -- expected, try the next resolver.
            }
        }

        # Show it is alive. The user should never have to wonder whether it hung.
        $elapsed = [int]((Get-Date) - $deadline.AddMinutes(-$TimeoutMinutes)).TotalSeconds
        Write-Host ("    ... not visible yet ({0}s elapsed, check {1})" -f $elapsed, $round) -ForegroundColor DarkGray
        Start-Sleep -Seconds 15
    }

    Write-Warn "timed out waiting for $Type $name after ${TimeoutMinutes}m"
    Write-Warn "  verify the record exists in the GoDaddy DNS panel, then re-run -- the script resumes from here."
    return $false
}

function Set-DnsRecord {
    <#
    .SYNOPSIS
        Create a DNS record the best way available: API if credentials exist,
        otherwise print instructions. Then wait for it to resolve either way.

    .OUTPUTS
        $true if the record is live in public DNS.
    #>
    param(
        [Parameter(Mandatory)][string]$Domain,
        [Parameter(Mandatory)][string]$FullName,   # fully qualified
        [Parameter(Mandatory)][ValidateSet('A', 'AAAA', 'CNAME', 'TXT')][string]$Type,
        [Parameter(Mandatory)][string]$Value,
        [string]$Purpose = '',
        [int]$TimeoutMinutes = 20
    )

    $relative = ConvertTo-RelativeName -FullName $FullName -Domain $Domain
    $viaApi = $false

    if (Test-GoDaddyApi) {
        $viaApi = Set-GoDaddyRecord -Domain $Domain -Name $relative -Type $Type -Value $Value
    } else {
        Write-Note 'No GoDaddy credentials (set GODADDY_PAT) -- manual DNS.'
    }

    if (-not $viaApi) {
        Show-ManualDnsRecord -Domain $Domain -Name $relative -Type $Type -Value $Value -Purpose $Purpose
        Write-Host '  Add the record above, then press Enter to continue (the script will poll DNS).' -ForegroundColor Yellow
        Read-Host '  Press Enter when added' | Out-Null
    }

    return (Wait-DnsRecord -FullName $FullName -Type $Type -ExpectedValue $Value -TimeoutMinutes $TimeoutMinutes)
}
