<#
.SYNOPSIS
    Shared helpers for standing up site infrastructure: the site registry, thin
    AWS CLI wrappers, and idempotent "get or create" functions for the CloudFront
    pieces an admin subdomain needs.

.DESCRIPTION
    Dot-source this from a script in scripts\:

        . "$PSScriptRoot\lib\SiteInfra.ps1"

    Every New-*/Get-OrNew-* function here is idempotent: it looks the resource up
    by name (or by alias, for distributions) before creating anything, so a script
    that dies halfway can simply be re-run.

    DNS lives in lib\GoDaddyDns.ps1 -- these domains are registered AND resolved at
    GoDaddy, not Route 53, so nothing here assumes a hosted zone.
#>

Set-StrictMode -Version Latest

# CloudFront's own hosted zone id, for Route 53 alias records. Constant, not a typo.
$script:CF_HOSTED_ZONE_ID = 'Z2FDTNDATAQYW2'

# AWS managed cache policies.
$script:CACHE_POLICY_OPTIMIZED = '658327ea-f89d-4fab-a63d-7e88639e58f6'
$script:CACHE_POLICY_DISABLED  = '4135ea2d-6df8-44a3-9df3-4b5a84be39ad'

# ---------------------------------------------------------------- console output

function Write-Step  { param($Number, $Message) Write-Host "`n[$Number] $Message" -ForegroundColor Cyan }
function Write-Ok    { param($Message) Write-Host "    $Message" -ForegroundColor Green }
function Write-Skip  { param($Message) Write-Host "    $Message (exists -- skipping)" -ForegroundColor DarkGray }
function Write-Note  { param($Message) Write-Host "    $Message" -ForegroundColor DarkGray }
function Write-Warn  { param($Message) Write-Host "    $Message" -ForegroundColor Yellow }

# ------------------------------------------------------------------ site registry

function Get-SiteRegistry {
    <#
    .SYNOPSIS
        Read scripts\sites.json.
    #>
    $path = Join-Path (Split-Path $PSScriptRoot -Parent) 'sites.json'
    if (-not (Test-Path $path)) { throw "Site registry not found at $path" }
    return (Get-Content $path -Raw | ConvertFrom-Json).sites
}

function Get-ExcludedSites {
    <#
    .SYNOPSIS
        Domains this tooling must not touch, with the reason each was excluded.
    #>
    $path = Join-Path (Split-Path $PSScriptRoot -Parent) 'sites.json'
    if (-not (Test-Path $path)) { return @() }
    $raw = (Get-Content $path -Raw | ConvertFrom-Json)
    if (-not $raw.PSObject.Properties['excluded']) { return @() }
    return @($raw.excluded)
}

function Resolve-Site {
    <#
    .SYNOPSIS
        Resolve a site key from the registry, or synthesise an entry from a raw
        domain so a brand-new site can be built before it is registered.

    .PARAMETER Site
        Registry key, e.g. ldsapologetics.

    .PARAMETER Domain
        Raw domain, e.g. newsite.com. Used when the site is not in sites.json yet.
    #>
    param([string]$Site, [string]$Domain)

    if ($Site) {
        $entry = Get-SiteRegistry | Where-Object { $_.key -eq $Site }
        if (-not $entry) {
            # Deliberately excluded sites get their recorded reason rather than a bare
            # "unknown key", which reads like an oversight and invites someone to re-add it.
            $blocked = Get-ExcludedSites | Where-Object { $_.key -eq $Site }
            if ($blocked) {
                throw "Site '$Site' is deliberately EXCLUDED from this tooling.`n`nReason: $($blocked.reason)`n`nIf that is genuinely no longer true, move the entry from 'excluded' back into 'sites' in scripts/sites.json."
            }
            $known = (Get-SiteRegistry | ForEach-Object { $_.key }) -join ', '
            throw "Unknown site key '$Site'. Known keys: $known. For a site that is not registered yet, pass -Domain instead."
        }
        return $entry
    }

    if ($Domain) {
        # Derive a dot-free key from the domain: newsite.com -> newsite
        $key = ($Domain -split '\.')[0] -replace '[^a-z0-9-]', ''
        # Shape must match a sites.json entry exactly -- Set-StrictMode makes a
        # missing property a hard error downstream, not a silent $null.
        return [pscustomobject]@{
            key = $key; id = 0; domain = $Domain
            title = $Domain; analytics = ''; dns = 'godaddy'
            configPrefix = $null
        }
    }

    throw 'Pass either -Site (a key from sites.json) or -Domain (for a new site).'
}

function Get-AdminBucketName {
    <#
    .SYNOPSIS
        Bucket name for a site's admin subdomain. Deliberately contains NO dots.

    .DESCRIPTION
        CloudFront always talks to an S3 REST origin over HTTPS, and the
        *.s3.{region}.amazonaws.com wildcard certificate does not match a
        multi-label host -- so a bucket literally named "admin.example.com" cannot
        be used as a CloudFront origin. The bucket-name-equals-domain rule only
        applies to S3 static website hosting, which we do not use here: the bucket
        is private and read through an Origin Access Control.
    #>
    param([Parameter(Mandatory)][string]$SiteKey)
    return "admin-$SiteKey"
}

# ------------------------------------------------------------------ aws cli glue

function Get-Prop {
    <#
    .SYNOPSIS
        Read a property that may legitimately be absent, returning $null instead of throwing.

    .DESCRIPTION
        Set-StrictMode -Version Latest makes accessing a non-existent property a hard
        error. That is usually what we want, but AWS responses are shaped by state:
        a freshly requested ACM certificate has no ResourceRecord until the service
        populates one, and a distribution list has no Items when the account has none.
        Those are answers to wait on or branch on, not failures.

        Supports dotted paths: Get-Prop $detail 'Certificate.DomainValidationOptions'
    #>
    param($Object, [Parameter(Mandatory)][string]$Path)

    $cur = $Object
    foreach ($part in $Path.Split('.')) {
        if ($null -eq $cur) { return $null }
        $prop = $cur.PSObject.Properties[$part]
        if (-not $prop) { return $null }
        $cur = $prop.Value
    }
    return $cur
}

function Invoke-Native {
    <#
    .SYNOPSIS
        Run a native executable and return its exit code + combined output, without
        letting PowerShell turn stderr into a terminating error.

    .DESCRIPTION
        Windows PowerShell 5.1 wraps every stderr line from a native command in an
        ErrorRecord. With $ErrorActionPreference = 'Stop' -- which these scripts set,
        deliberately, so a genuine failure halts the run -- that ErrorRecord becomes a
        TERMINATING error, killing the script even when the command exited 0, and even
        when stderr was redirected to $null.

        That breaks any command whose failure is a legitimate answer. `aws s3api
        head-bucket` on a bucket that does not exist yet is exactly that: it writes to
        stderr and exits non-zero, which is how we learn the bucket needs creating.

        So: neutralise ErrorActionPreference for the duration of the call and judge the
        result by $LASTEXITCODE, which is what actually means "did this work".
    #>
    param(
        [Parameter(Mandatory)][string]$Exe,
        [string[]]$Arguments = @()
    )
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $Exe @Arguments 2>&1
        $code = $LASTEXITCODE
        # Drop the ErrorRecord wrappers 5.1 adds around stderr lines; keep their text.
        $lines = @($out | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { $_ }
        })
        return [pscustomobject]@{ ExitCode = $code; Output = $lines }
    }
    finally { $ErrorActionPreference = $prev }
}

function Invoke-NativeStreaming {
    <#
    .SYNOPSIS
        Like Invoke-Native, but writes output to the console AS IT ARRIVES.
        Returns the exit code.

    .DESCRIPTION
        Invoke-Native buffers everything and returns it at the end, which is right for
        short AWS calls whose output we parse. It is wrong for anything slow and
        interactive: an `npm run build` takes minutes, and buffering makes it look hung
        with no way to tell a compile from a crash.

        Same stderr protection as Invoke-Native -- ErrorActionPreference is neutralised
        so 5.1 cannot promote a progress message on stderr to a terminating error.
    #>
    param(
        [Parameter(Mandatory)][string]$Exe,
        [string[]]$Arguments = @(),
        [string]$Indent = '    '
    )
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & $Exe @Arguments 2>&1 | ForEach-Object {
            $line = if ($_ -is [System.Management.Automation.ErrorRecord]) { $_.ToString() } else { "$_" }
            if ($line.Trim()) { Write-Host "$Indent$line" -ForegroundColor DarkGray }
        }
        return $LASTEXITCODE
    }
    finally { $ErrorActionPreference = $prev }
}

function Add-DefaultRegion {
    <#
    .SYNOPSIS
        Ensure an AWS CLI argument list carries a --region.

    .DESCRIPTION
        CloudFront is a global service, but the CLI still refuses to run without a
        region ("NoRegion: You must specify a region"). Whether it works therefore
        depends on AWS_REGION happening to be set in the shell -- which some of these
        scripts set as a side effect and others do not, so the same command succeeds in
        one session and fails in the next. That is the worst kind of flakiness, so
        pin it here rather than relying on ambient state.

        CloudFront's control plane is always us-east-1. Anything else falls back to
        the AWS_REGION/AWS_DEFAULT_REGION in the environment, or us-west-2.
    #>
    param([Parameter(Mandatory)][string[]]$Arguments)

    if ($Arguments -contains '--region') { return $Arguments }
    if ($Arguments.Count -eq 0) { return $Arguments }

    $region = if ($Arguments[0] -eq 'cloudfront') { 'us-east-1' }
              elseif ($env:AWS_REGION) { $env:AWS_REGION }
              elseif ($env:AWS_DEFAULT_REGION) { $env:AWS_DEFAULT_REGION }
              else { 'us-west-2' }

    return $Arguments + @('--region', $region)
}

function Invoke-Aws {
    <#
    .SYNOPSIS
        Run the AWS CLI, throw on failure, parse JSON output when there is any.
    #>
    param([Parameter(Mandatory)][string[]]$Arguments)

    $Arguments = Add-DefaultRegion $Arguments
    $r = Invoke-Native -Exe 'aws' -Arguments $Arguments
    if ($r.ExitCode -ne 0) {
        throw "aws $($Arguments -join ' ') failed (exit $($r.ExitCode))`n$($r.Output -join "`n")"
    }
    if (-not $r.Output) { return $null }
    $text = ($r.Output -join "`n").Trim()
    if (-not $text) { return $null }
    try { return $text | ConvertFrom-Json } catch { return $text }
}

function Test-AwsResource {
    <#
    .SYNOPSIS
        Run an AWS CLI command whose FAILURE is a meaningful answer (does this
        bucket/object exist?). Returns $true on exit 0, $false otherwise. Never throws.
    #>
    param([Parameter(Mandatory)][string[]]$Arguments)
    return ((Invoke-Native -Exe 'aws' -Arguments (Add-DefaultRegion $Arguments)).ExitCode -eq 0)
}

function Save-JsonArg {
    <#
    .SYNOPSIS
        Serialise an object to a temp file and return a file:// URI for the CLI.

    .DESCRIPTION
        Passing large JSON inline to aws.exe on Windows is a quoting minefield;
        file:// sidesteps it entirely.
    #>
    param(
        [Parameter(Mandatory)]$Object,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$WorkDir
    )
    if (-not (Test-Path $WorkDir)) { New-Item -ItemType Directory -Path $WorkDir -Force | Out-Null }
    $path = Join-Path $WorkDir $Name

    # MUST be UTF-8 WITHOUT a BOM. Out-File -Encoding utf8 on Windows PowerShell 5.1
    # emits EF BB BF, and the AWS CLI fails to parse a file:// payload that starts with
    # it: "Expected: '=', received: '∩'". Write the bytes ourselves instead.
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, ($Object | ConvertTo-Json -Depth 20), $utf8NoBom)

    return "file://$($path -replace '\\','/')"
}

function Test-AwsAuth {
    <#
    .SYNOPSIS
        Fail early and clearly if the profile's session has expired.
    #>
    param([Parameter(Mandatory)][string]$AwsProfile, [string]$Region = 'us-west-2')
    try {
        return Invoke-Aws @('sts', 'get-caller-identity', '--profile', $AwsProfile, '--region', $Region, '--output', 'json')
    } catch {
        throw "AWS profile '$AwsProfile' is not authenticated (session expired?). Re-auth and re-run.`n$_"
    }
}

# --------------------------------------------------------------------- s3 bucket

function New-PrivateBucket {
    <#
    .SYNOPSIS
        Create (if needed) a private bucket with all public access blocked.
        Static website hosting is deliberately NOT enabled -- CloudFront reads
        the bucket through OAC, and website endpoints are HTTP-only anyway.
    #>
    param(
        [Parameter(Mandatory)][string]$Bucket,
        [Parameter(Mandatory)][string]$AwsProfile,
        [string]$Region = 'us-west-2'
    )

    # A missing bucket is the expected case on first run, so this must not throw.
    if (Test-AwsResource @('s3api', 'head-bucket', '--bucket', $Bucket, '--profile', $AwsProfile, '--region', $Region)) {
        Write-Skip "s3://$Bucket"
    } else {
        # Not $args -- that is an automatic variable.
        $createArgs = @('s3api', 'create-bucket', '--bucket', $Bucket, '--region', $Region, '--profile', $AwsProfile)
        # us-east-1 is the one region that rejects an explicit LocationConstraint.
        if ($Region -ne 'us-east-1') { $createArgs += @('--create-bucket-configuration', "LocationConstraint=$Region") }
        Invoke-Aws $createArgs | Out-Null
        Write-Ok "created s3://$Bucket"
    }

    Invoke-Aws @('s3api', 'put-public-access-block', '--bucket', $Bucket,
        '--profile', $AwsProfile, '--region', $Region,
        '--public-access-block-configuration',
        'BlockPublicAcls=true,IgnorePublicAcls=true,BlockPublicPolicy=true,RestrictPublicBuckets=true') | Out-Null
    Write-Ok 'public access blocked'
}

function Set-CloudFrontOriginBucketPolicy {
    <#
    .SYNOPSIS
        Grant cloudfront.amazonaws.com s3:GetObject on the bucket, scoped by
        AWS:SourceArn to one distribution.

    .DESCRIPTION
        This must run AFTER the distribution exists -- the condition needs its ARN.
        That ordering is the only genuinely awkward part of doing this by CLI; the
        console hides it behind a "copy policy" button.
    #>
    param(
        [Parameter(Mandatory)][string]$Bucket,
        [Parameter(Mandatory)][string]$DistributionArn,
        [Parameter(Mandatory)][string]$AwsProfile,
        [string]$Region = 'us-west-2',
        [Parameter(Mandatory)][string]$WorkDir
    )

    $policy = @{
        Version = '2008-10-17'
        Id      = 'PolicyForCloudFrontPrivateContent'
        Statement = @(@{
            Sid       = 'AllowCloudFrontServicePrincipal'
            Effect    = 'Allow'
            Principal = @{ Service = 'cloudfront.amazonaws.com' }
            Action    = 's3:GetObject'
            Resource  = "arn:aws:s3:::$Bucket/*"
            Condition = @{ StringEquals = @{ 'AWS:SourceArn' = $DistributionArn } }
        })
    }
    Invoke-Aws @('s3api', 'put-bucket-policy', '--bucket', $Bucket,
        '--policy', (Save-JsonArg $policy 'bucket-policy.json' $WorkDir),
        '--profile', $AwsProfile, '--region', $Region) | Out-Null
    Write-Ok "s3://$Bucket readable only by distribution $DistributionArn"
}

# ------------------------------------------------------------------ acm (us-east-1)

function Get-OrNewCertificate {
    <#
    .SYNOPSIS
        Find an existing usable certificate for a host, or request one.

    .DESCRIPTION
        CloudFront requires the certificate in us-east-1 regardless of where the
        bucket lives. Returns an object with:
            Arn             the certificate ARN
            IsIssued        $true if already validated
            ValidationName  DNS record name to create (when not yet issued)
            ValidationValue DNS record value

        A wildcard cert for the parent domain is accepted if one exists, since
        *.example.com covers admin.example.com.
    #>
    param(
        [Parameter(Mandatory)][string]$HostName,
        [Parameter(Mandatory)][string]$Domain,
        [Parameter(Mandatory)][string]$AwsProfile
    )

    # Poll describe-certificate until ACM has populated the DNS validation record.
    # It is absent for the first few seconds after request-certificate, and absent
    # properties throw under StrictMode -- hence Get-Prop rather than direct access.
    function Wait-ValidationRecord {
        param([string]$Arn, [string]$Profile, [int]$Attempts = 20)
        for ($i = 0; $i -lt $Attempts; $i++) {
            $detail = Invoke-Aws @('acm', 'describe-certificate', '--certificate-arn', $Arn,
                '--region', 'us-east-1', '--profile', $Profile, '--output', 'json')
            $opts = @(Get-Prop $detail 'Certificate.DomainValidationOptions')
            if ($opts.Count -gt 0) {
                $rr = Get-Prop $opts[0] 'ResourceRecord'
                if ($rr) { return $rr }
            }
            Start-Sleep -Seconds 3
        }
        return $null
    }

    $list = Invoke-Aws @('acm', 'list-certificates', '--region', 'us-east-1',
        '--profile', $AwsProfile, '--certificate-statuses', 'ISSUED', 'PENDING_VALIDATION',
        '--output', 'json')

    $existing = @(Get-Prop $list 'CertificateSummaryList') |
        Where-Object { $_ -and ($_.DomainName -eq $HostName -or $_.DomainName -eq "*.$Domain") } |
        Select-Object -First 1

    if ($existing) {
        $detail = Invoke-Aws @('acm', 'describe-certificate', '--certificate-arn', $existing.CertificateArn,
            '--region', 'us-east-1', '--profile', $AwsProfile, '--output', 'json')
        if ((Get-Prop $detail 'Certificate.Status') -eq 'ISSUED') {
            return [pscustomobject]@{ Arn = $existing.CertificateArn; IsIssued = $true }
        }
        Write-Note "reusing pending certificate $($existing.CertificateArn)"
        $rr = Wait-ValidationRecord -Arn $existing.CertificateArn -Profile $AwsProfile
        if (-not $rr) { throw "ACM has no DNS validation record for the existing certificate $($existing.CertificateArn). Delete it in ACM (us-east-1) and re-run to request a fresh one." }
        return [pscustomobject]@{
            Arn = $existing.CertificateArn; IsIssued = $false
            ValidationName = $rr.Name; ValidationValue = $rr.Value
        }
    }

    $arn = Invoke-Aws @('acm', 'request-certificate', '--domain-name', $HostName,
        '--validation-method', 'DNS', '--region', 'us-east-1', '--profile', $AwsProfile,
        '--query', 'CertificateArn', '--output', 'text')
    Write-Ok "requested certificate $arn"

    $rr = Wait-ValidationRecord -Arn $arn -Profile $AwsProfile
    if (-not $rr) { throw "ACM never returned a DNS validation record for $HostName." }

    return [pscustomobject]@{
        Arn = $arn; IsIssued = $false
        ValidationName = $rr.Name; ValidationValue = $rr.Value
    }
}

function Wait-CertificateIssued {
    param(
        [Parameter(Mandatory)][string]$CertificateArn,
        [Parameter(Mandatory)][string]$AwsProfile,
        [int]$TimeoutMinutes = 30
    )
    Write-Note "waiting for ACM to see the DNS record and issue the certificate..."
    $deadline = (Get-Date).AddMinutes($TimeoutMinutes)
    while ((Get-Date) -lt $deadline) {
        $d = Invoke-Aws @('acm', 'describe-certificate', '--certificate-arn', $CertificateArn,
            '--region', 'us-east-1', '--profile', $AwsProfile, '--output', 'json')
        switch (Get-Prop $d 'Certificate.Status') {
            'ISSUED' { Write-Ok 'certificate ISSUED'; return }
            'FAILED' { throw "Certificate validation FAILED: $(Get-Prop $d 'Certificate.FailureReason')" }
            'VALIDATION_TIMED_OUT' { throw 'Certificate validation timed out at ACM (72h). Request a new one.' }
        }
        Start-Sleep -Seconds 20
    }
    throw "Certificate still not issued after $TimeoutMinutes minutes. The DNS record is usually the culprit -- verify it at GoDaddy."
}

# --------------------------------------------------------------------- cloudfront

function Get-OrNewOriginAccessControl {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$Description,
        [Parameter(Mandatory)][string]$AwsProfile,
        [Parameter(Mandatory)][string]$WorkDir
    )

    $list = Invoke-Aws @('cloudfront', 'list-origin-access-controls', '--profile', $AwsProfile, '--output', 'json')
    $found = @(Get-Prop $list 'OriginAccessControlList.Items') |
        Where-Object { $_ -and $_.Name -eq $Name } | Select-Object -First 1
    if ($found) { Write-Skip "OAC $Name ($($found.Id))"; return $found.Id }

    $cfg = @{
        Name                          = $Name
        Description                   = $Description
        SigningProtocol               = 'sigv4'
        SigningBehavior               = 'always'
        OriginAccessControlOriginType = 's3'
    }
    $id = (Invoke-Aws @('cloudfront', 'create-origin-access-control',
        '--origin-access-control-config', (Save-JsonArg $cfg 'oac.json' $WorkDir),
        '--profile', $AwsProfile, '--output', 'json')).OriginAccessControl.Id
    Write-Ok "created OAC $Name ($id)"
    return $id
}

function Get-OrNewNoIndexHeadersPolicy {
    <#
    .SYNOPSIS
        A response headers policy adding X-Robots-Tag: noindex, nofollow plus
        baseline security headers. Shared by every site's admin subdomain, so it
        is created once and reused.

    .DESCRIPTION
        robots.txt asks well-behaved crawlers not to fetch; X-Robots-Tag tells
        anything that fetched anyway not to index. The admin console must never
        show up in search results, so we do both.
    #>
    param(
        [Parameter(Mandatory)][string]$AwsProfile,
        [Parameter(Mandatory)][string]$WorkDir,
        [string]$Name = 'admin-noindex'
    )

    $list = Invoke-Aws @('cloudfront', 'list-response-headers-policies', '--type', 'custom',
        '--profile', $AwsProfile, '--output', 'json')
    $found = @(Get-Prop $list 'ResponseHeadersPolicyList.Items') |
        Where-Object { $_ -and (Get-Prop $_ 'ResponseHeadersPolicy.ResponseHeadersPolicyConfig.Name') -eq $Name } |
        Select-Object -First 1
    if ($found) { Write-Skip "headers policy $Name ($($found.ResponseHeadersPolicy.Id))"; return $found.ResponseHeadersPolicy.Id }

    $cfg = @{
        Name    = $Name
        Comment = 'Admin subdomains: keep out of search indexes'
        CustomHeadersConfig = @{
            Quantity = 1
            Items    = @(@{ Header = 'X-Robots-Tag'; Value = 'noindex, nofollow'; Override = $true })
        }
        SecurityHeadersConfig = @{
            ContentTypeOptions      = @{ Override = $true }
            FrameOptions            = @{ FrameOption = 'SAMEORIGIN'; Override = $true }
            ReferrerPolicy          = @{ ReferrerPolicy = 'strict-origin-when-cross-origin'; Override = $true }
            StrictTransportSecurity = @{ AccessControlMaxAgeSec = 31536000; IncludeSubdomains = $true; Preload = $false; Override = $true }
        }
    }
    $id = (Invoke-Aws @('cloudfront', 'create-response-headers-policy',
        '--response-headers-policy-config', (Save-JsonArg $cfg 'response-headers-policy.json' $WorkDir),
        '--profile', $AwsProfile, '--output', 'json')).ResponseHeadersPolicy.Id
    Write-Ok "created headers policy $Name ($id)"
    return $id
}

function Get-DistributionByAlias {
    param(
        [Parameter(Mandatory)][string]$Alias,
        [Parameter(Mandatory)][string]$AwsProfile
    )
    $list = Invoke-Aws @('cloudfront', 'list-distributions', '--profile', $AwsProfile, '--output', 'json')
    foreach ($d in @(Get-Prop $list 'DistributionList.Items')) {
        if (-not $d) { continue }
        $aliases = @(Get-Prop $d 'Aliases.Items')
        if ($aliases -contains $Alias) { return $d }
    }
    return $null
}

function Get-OrNewSpaDistribution {
    <#
    .SYNOPSIS
        Create (if needed) a CloudFront distribution fronting a private S3 bucket
        that holds a single-page app.

    .DESCRIPTION
        The two settings that matter and are easy to get wrong:

        * CustomErrorResponses map BOTH 403 and 404 to /index.html with a 200.
          A private bucket answers a missing key with 403, not 404, so mapping
          only 404 leaves every SPA deep link showing an AccessDenied page. The
          200 matters too -- returning the shell with a 403/404 status makes the
          router (and the browser) treat it as an error.

        * S3OriginConfig.OriginAccessIdentity is empty AND OriginAccessControlId
          is set. Empty OAI means "not the legacy OAI mechanism"; the OAC id is
          what actually grants access.
    #>
    param(
        [Parameter(Mandatory)][string]$Alias,
        [Parameter(Mandatory)][string]$Bucket,
        [Parameter(Mandatory)][string]$BucketRegion,
        [Parameter(Mandatory)][string]$CertificateArn,
        [Parameter(Mandatory)][string]$OacId,
        [Parameter(Mandatory)][string]$ResponseHeadersPolicyId,
        [Parameter(Mandatory)][string]$CallerReference,
        [Parameter(Mandatory)][string]$AwsProfile,
        [Parameter(Mandatory)][string]$WorkDir,
        [string]$Comment = '',
        [string]$PriceClass = 'PriceClass_100'
    )

    $existing = Get-DistributionByAlias -Alias $Alias -AwsProfile $AwsProfile
    if ($existing) {
        Write-Skip "distribution for $Alias ($($existing.Id))"
        return [pscustomobject]@{ Id = $existing.Id; DomainName = $existing.DomainName; Arn = $existing.ARN }
    }

    $originId = 's3-spa-origin'
    $cfg = @{
        CallerReference   = $CallerReference
        Comment           = $Comment
        Enabled           = $true
        Aliases           = @{ Quantity = 1; Items = @($Alias) }
        DefaultRootObject = 'index.html'
        Origins = @{
            Quantity = 1
            Items = @(@{
                Id                    = $originId
                DomainName            = "$Bucket.s3.$BucketRegion.amazonaws.com"
                OriginPath            = ''
                CustomHeaders         = @{ Quantity = 0 }
                S3OriginConfig        = @{ OriginAccessIdentity = '' }
                OriginAccessControlId = $OacId
                ConnectionAttempts    = 3
                ConnectionTimeout     = 10
            })
        }
        DefaultCacheBehavior = @{
            TargetOriginId          = $originId
            ViewerProtocolPolicy    = 'redirect-to-https'
            Compress                = $true
            CachePolicyId           = $script:CACHE_POLICY_OPTIMIZED
            ResponseHeadersPolicyId = $ResponseHeadersPolicyId
            AllowedMethods = @{
                Quantity      = 2
                Items         = @('HEAD', 'GET')
                CachedMethods = @{ Quantity = 2; Items = @('HEAD', 'GET') }
            }
            SmoothStreaming            = $false
            FieldLevelEncryptionId     = ''
            TrustedSigners             = @{ Enabled = $false; Quantity = 0 }
            TrustedKeyGroups           = @{ Enabled = $false; Quantity = 0 }
            LambdaFunctionAssociations = @{ Quantity = 0 }
            FunctionAssociations       = @{ Quantity = 0 }
        }
        CustomErrorResponses = @{
            Quantity = 2
            Items = @(
                @{ ErrorCode = 403; ResponsePagePath = '/index.html'; ResponseCode = '200'; ErrorCachingMinTTL = 0 }
                @{ ErrorCode = 404; ResponsePagePath = '/index.html'; ResponseCode = '200'; ErrorCachingMinTTL = 0 }
            )
        }
        ViewerCertificate = @{
            ACMCertificateArn            = $CertificateArn
            SSLSupportMethod             = 'sni-only'
            MinimumProtocolVersion       = 'TLSv1.2_2021'
            CloudFrontDefaultCertificate = $false
        }
        HttpVersion   = 'http2and3'
        IsIPV6Enabled = $true
        PriceClass    = $PriceClass
        Restrictions  = @{ GeoRestriction = @{ RestrictionType = 'none'; Quantity = 0 } }
        Logging       = @{ Enabled = $false; IncludeCookies = $false; Bucket = ''; Prefix = '' }
        WebACLId      = ''
    }

    $created = Invoke-Aws @('cloudfront', 'create-distribution',
        '--distribution-config', (Save-JsonArg $cfg 'distribution.json' $WorkDir),
        '--profile', $AwsProfile, '--output', 'json')
    Write-Ok "created distribution $($created.Distribution.Id) ($($created.Distribution.DomainName))"
    return [pscustomobject]@{
        Id         = $created.Distribution.Id
        DomainName = $created.Distribution.DomainName
        Arn        = $created.Distribution.ARN
    }
}
