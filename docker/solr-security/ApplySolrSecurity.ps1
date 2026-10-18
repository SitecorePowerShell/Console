param(
    [string]$Username = $env:SOLR_ADMIN_USERNAME,
    [string]$Password = $env:SOLR_ADMIN_PASSWORD,
    [string]$SolrBaseUrl = $env:SOLR_SECURITY_URL,
    [string]$ZkHost = $env:SOLR_ZK_HOST,
    [string]$SecurityJsonPath = $env:SOLR_SECURITY_JSON_PATH,
    [string]$ZkSecurityPath = 'zk:/security.json'
)

# Compose post_start hooks often don't stream to `docker logs`; transcript goes to C:\data (.\solr-data on the host).
$dataDir = 'C:\data'
if (-not (Test-Path -LiteralPath $dataDir)) {
    New-Item -ItemType Directory -Path $dataDir -Force | Out-Null
}
$logPath = Join-Path $dataDir 'ApplySolrSecurity-post-start.log'
$script:LogStartTime = Get-Date
Stop-Transcript -ErrorAction SilentlyContinue | Out-Null
Start-Transcript -Path $logPath -Append | Out-Null

function Format-SolrSecurityLogTimestamps {
    param(
        [string]$Path,
        [datetime]$StartTime,
        [datetime]$EndTime
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $startLine = 'Start time: {0:yyyy-MM-dd HH:mm:ss}' -f $StartTime
    $endLine = 'End time: {0:yyyy-MM-dd HH:mm:ss}' -f $EndTime
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.AddRange([string[]](Get-Content -LiteralPath $Path))

    for ($i = $lines.Count - 1; $i -ge 0; $i--) {
        if ($lines[$i] -match '^Start time: \d+\s*$') {
            $lines[$i] = $startLine
            break
        }
    }
    for ($i = $lines.Count - 1; $i -ge 0; $i--) {
        if ($lines[$i] -match '^End time: \d+\s*$') {
            $lines[$i] = $endLine
            break
        }
    }

    [System.IO.File]::WriteAllLines($Path, $lines, [System.Text.UTF8Encoding]::new($false))
}

function Complete-SolrSecurityScript {
    param(
        [int]$ExitCode = 0
    )
    
    $endTime = Get-Date
    Stop-Transcript -ErrorAction SilentlyContinue | Out-Null
    Format-SolrSecurityLogTimestamps -Path $logPath -StartTime $script:LogStartTime -EndTime $endTime
    exit $ExitCode
}

function New-SolrBasicAuthCredential {
    param(
        [string]$Password
    )
    
    # Match org.apache.solr.security.Sha256AuthenticationProvider.getSaltedHashedValue / sha256(password, saltBase64)
    $rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    $saltRaw = New-Object byte[] 32
    $rng.GetBytes($saltRaw)
    $saltB64 = [Convert]::ToBase64String($saltRaw)
    $saltDecoded = [Convert]::FromBase64String($saltB64)
    $pwBytes = [System.Text.Encoding]::UTF8.GetBytes($Password)
    $combined = New-Object byte[] ($saltDecoded.Length + $pwBytes.Length)
    [System.Buffer]::BlockCopy($saltDecoded, 0, $combined, 0, $saltDecoded.Length)
    [System.Buffer]::BlockCopy($pwBytes, 0, $combined, $saltDecoded.Length, $pwBytes.Length)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    $first = $sha.ComputeHash($combined)
    $second = [System.Security.Cryptography.SHA256]::Create().ComputeHash($first)
    $hashB64 = [Convert]::ToBase64String($second)
    return ($hashB64 + ' ' + $saltB64)
}

function Resolve-SolrCmd {
    $candidates = @()
    if ($env:SOLR_INSTALL_DIR) {
        $candidates += Join-Path $env:SOLR_INSTALL_DIR 'bin\solr.cmd'
    }
    $candidates += 'C:\solr\bin\solr.cmd'
    foreach ($p in $candidates) {
        if ($p -and (Test-Path -LiteralPath $p)) {
            return $p
        }
    }
    Write-Error "Couldn't find Solr CLI (solr.cmd). Set SOLR_INSTALL_DIR or verify the Solr installation path."
    Complete-SolrSecurityScript 1
}

function Invoke-SolrCli {
    param(
        [string]$SolrCmd,
        [Parameter(Mandatory = $true)]
        [string[]]$ArgumentList,
        [string]$WorkingDirectory
    )

    # Run via cmd with redirected streams so Solr ZK WARN lines on stderr do not become PowerShell errors in the transcript.
    $quotedSolr = '"' + ($SolrCmd -replace '"', '""') + '"'
    $quotedArgs = ($ArgumentList | ForEach-Object {
        $arg = [string]$_
        if ($arg -match '[\s"]') {
            '"' + ($arg -replace '"', '""') + '"'
        }
        else {
            $arg
        }
    }) -join ' '

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = 'cmd.exe'
    $psi.Arguments = "/d /c $quotedSolr $quotedArgs"
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.UseShellExecute = $false
    $psi.CreateNoWindow = $true
    if ($WorkingDirectory) {
        $psi.WorkingDirectory = $WorkingDirectory
    }

    $process = [System.Diagnostics.Process]::Start($psi)
    $stdout = $process.StandardOutput.ReadToEnd()
    $null = $process.StandardError.ReadToEnd()
    $process.WaitForExit()

    return [pscustomobject]@{
        ExitCode = $process.ExitCode
        Output   = $stdout
    }
}

function Wait-ZkReady {
    param(
        [string]$SolrCmd,
        [string]$ZkHost
    )

    Write-Host "INFO: Waiting for ZooKeeper at $ZkHost ..."
    for ($i = 0; $i -lt 90; $i++) {
        $result = Invoke-SolrCli -SolrCmd $SolrCmd -ArgumentList @('zk', 'ls', '/', '-z', $ZkHost)
        if ($result.ExitCode -eq 0) {
            Write-Host 'INFO: ZooKeeper is ready.'
            return
        }
        Start-Sleep -Seconds 2
    }
    Write-Error "ZooKeeper at $ZkHost didn't become ready within timeout."
    Complete-SolrSecurityScript 1
}

function Check-ZkSecurityJsonConfigured {
    param(
        [string]$SolrCmd,
        [string]$ZkHost,
        [string]$ZkSecurityPath
    )

    # Solr may create an empty /security.json znode on startup; only skip bootstrap when auth is actually configured.
    $ls = Invoke-SolrCli -SolrCmd $SolrCmd -ArgumentList @('zk', 'ls', '/', '-z', $ZkHost)
    if ($ls.ExitCode -ne 0) {
        return $false
    }
    $hasNode = $false
    foreach ($line in ($ls.Output -split "`r?`n")) {
        if (($line -as [string]).Trim() -eq 'security.json') {
            $hasNode = $true
            break
        }
    }
    if (-not $hasNode) {
        return $false
    }

    $probeDir = [System.IO.Path]::GetTempPath()
    $probeName = ('solr-security-zk-probe-{0}.json' -f [Guid]::NewGuid().ToString('n'))
    $probePath = Join-Path $probeDir $probeName
    try {
        $cp = Invoke-SolrCli -SolrCmd $SolrCmd -WorkingDirectory $probeDir -ArgumentList @(
            'zk', 'cp', $ZkSecurityPath, ('./{0}' -f $probeName), '-z', $ZkHost
        )
        if ($cp.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $probePath)) {
            return $false
        }
        $raw = Get-Content -Raw -LiteralPath $probePath
        if ([string]::IsNullOrWhiteSpace($raw)) {
            return $false
        }
        # Use pattern checks instead of ConvertFrom-Json (empty/placeholder znodes and PS 5.1 name issues).
        if ($raw -notmatch '(?i)"authentication"\s*:\s*\{') {
            return $false
        }
        if ($raw -notmatch '(?i)"class"\s*:\s*"\s*solr\.[^"]+"') {
            return $false
        }
        if ($raw -notmatch '(?i)"credentials"\s*:\s*\{\s*"[^"]+"\s*:') {
            return $false
        }
        return $true
    }
    finally {
        if (Test-Path -LiteralPath $probePath) {
            Remove-Item -LiteralPath $probePath -Force -ErrorAction SilentlyContinue
        }
    }
}

if (-not $ZkHost) { $ZkHost = 'localhost:9983' }
if (-not $SolrBaseUrl) { $SolrBaseUrl = 'http://localhost:8983' }
if (-not $SecurityJsonPath) { $SecurityJsonPath = Join-Path $PSScriptRoot 'security.json' }

$solrCmd = Resolve-SolrCmd
Wait-ZkReady -SolrCmd $solrCmd -ZkHost $ZkHost

if (Check-ZkSecurityJsonConfigured -SolrCmd $solrCmd -ZkHost $ZkHost -ZkSecurityPath $ZkSecurityPath) {
    Write-Host "INFO: /security.json in ZooKeeper at $ZkHost already has authentication configured; skipping security bootstrap upload."
    Complete-SolrSecurityScript 0
}

if (-not $Username) { Write-Error 'Username is required (-Username or env SOLR_ADMIN_USERNAME).'; Complete-SolrSecurityScript 1 }
if (-not $Password) { Write-Error 'Password is required (-Password or env SOLR_ADMIN_PASSWORD).'; Complete-SolrSecurityScript 1 }

$SolrBaseUrl = $SolrBaseUrl.TrimEnd('/')

if (-not (Test-Path -LiteralPath $SecurityJsonPath)) {
    Write-Error "Missing security.json at '$SecurityJsonPath'."
    Complete-SolrSecurityScript 1
}

$spec = Get-Content -Raw -LiteralPath $SecurityJsonPath | ConvertFrom-Json
$authz = $spec.authorization
$adminUserRoleName = 'sitecoreSolrAdminUserRole'
$generalUserRoleName = 'sitecoreGeneralUserRole'
if ($spec.PSObject.Properties.Name -contains 'solrUserRole' -and $spec.solrUserRole) {
    $adminUserRoleName = [string]$spec.solrUserRole
}

$credHash = New-SolrBasicAuthCredential -Password $Password
$creds = [ordered]@{}
$creds[$Username] = $credHash

$userRole = [ordered]@{}
$userRole[$Username] = @($adminUserRoleName, $generalUserRoleName)

$document = [ordered]@{
    authentication = [ordered]@{
        class        = 'solr.BasicAuthPlugin'
        blockUnknown = $true
        credentials  = $creds
    }
    authorization  = [ordered]@{
        class       = $authz.class
        'user-role' = $userRole
        permissions = $authz.permissions
    }
}

$json = $document | ConvertTo-Json -Depth 12 -Compress
$outFile = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), ('solr-security-zk-{0}.json' -f [Guid]::NewGuid().ToString('n')))
try {
    [System.IO.File]::WriteAllText($outFile, $json, [System.Text.UTF8Encoding]::new($false))

    $localOut = $outFile -replace '\\', '/'
    Write-Host "INFO: Uploading merged security (from '$SecurityJsonPath') to ZooKeeper at $ZkHost ($ZkSecurityPath) using $solrCmd ..."

    $zkCp = Invoke-SolrCli -SolrCmd $solrCmd -ArgumentList @('zk', 'cp', $localOut, $ZkSecurityPath, '-z', $ZkHost)
    if ($zkCp.ExitCode -ne 0) {
        Write-Error "solr zk cp failed with exit code $($zkCp.ExitCode)."
        Complete-SolrSecurityScript $zkCp.ExitCode
    }

    Write-Host 'INFO: Solr security configuration applied (ZK).'
    Complete-SolrSecurityScript 0
}
finally {
    if ($outFile -and (Test-Path -LiteralPath $outFile)) {
        Remove-Item -LiteralPath $outFile -Force -ErrorAction SilentlyContinue
    }
}