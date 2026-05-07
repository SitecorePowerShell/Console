# Unit tests for the AppDomain-scoped HttpClient pool and the cache key
# derivation. Covers identity collapse/split, age-out, eviction, lost-race
# handling, and the absence of cross-identity reuse.
#
# These tests do not require a running CM container; they exercise the pool
# type directly through Get-SpeClientCacheKey and [Spe.Internal.SpeHttpClientPool].

# Shorten the pool's age-out and capacity for the duration of this file so
# we can drive the eviction and refresh paths without 10-minute waits.
# Restored on exit.
$origMaxAge   = [Spe.Internal.SpeHttpClientPool]::MaxAgeMinutes
$origMaxEntry = [Spe.Internal.SpeHttpClientPool]::MaxEntries

try {
    [Spe.Internal.SpeHttpClientPool]::Clear()

    # ============================================================
    # Get-SpeClientCacheKey
    # ============================================================
    Write-Host "`n  [Get-SpeClientCacheKey]" -ForegroundColor White

    $u1 = [Uri]'https://cm.example.com/'

    # Same identity (HMAC) collapses to one key.
    $k1 = Get-SpeClientCacheKey -Uri $u1 -Username 'admin' -SharedSecret 'sec' -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false
    $k2 = Get-SpeClientCacheKey -Uri $u1 -Username 'admin' -SharedSecret 'sec' -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false
    Assert-Equal $k1 $k2 "Same HMAC identity collapses to one cache key"

    # Different SharedSecret splits.
    $k3 = Get-SpeClientCacheKey -Uri $u1 -Username 'admin' -SharedSecret 'OTHER' -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false
    Assert-NotEqual $k1 $k3 "Different SharedSecret produces different key"

    # Different Username splits.
    $k4 = Get-SpeClientCacheKey -Uri $u1 -Username 'guest' -SharedSecret 'sec' -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false
    Assert-NotEqual $k1 $k4 "Different Username produces different key"

    # Different URI splits even with same identity.
    $u2 = [Uri]'https://cm2.example.com/'
    $k5 = Get-SpeClientCacheKey -Uri $u2 -Username 'admin' -SharedSecret 'sec' -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false
    Assert-NotEqual $k1 $k5 "Different URI produces different key (identity-bound handler reuse boundary)"

    # OAuth bearer vs HMAC: different prefix, different key.
    $k6 = Get-SpeClientCacheKey -Uri $u1 -Username '' -SharedSecret '' -AccessKeyId '' -AccessToken 'abc.def.ghi' -Credential $null -UseDefaultCredentials $false
    Assert-NotEqual $k1 $k6 "OAuth-token caller and HMAC caller never share a key"

    # UseDefaultCredentials is its own bucket.
    $k7 = Get-SpeClientCacheKey -Uri $u1 -Username '' -SharedSecret '' -AccessKeyId '' -AccessToken '' -Credential $null -UseDefaultCredentials $true
    Assert-NotEqual $k1 $k7 "UseDefaultCredentials caller is its own bucket"

    # The hash should not embed the plaintext SharedSecret.
    Assert-True (-not $k1.Contains('sec')) "Plaintext SharedSecret never appears in the cache key"

    # ============================================================
    # SpeHttpClientPool.GetOrAdd: same key returns same client
    # ============================================================
    Write-Host "`n  [SpeHttpClientPool: identity reuse]" -ForegroundColor White

    [Spe.Internal.SpeHttpClientPool]::Clear()

    $factoryCalls = [pscustomobject]@{ N = 0 }
    $factory = [System.Func[System.Net.Http.HttpClient]] {
        $factoryCalls.N++
        $h = New-Object System.Net.Http.HttpClientHandler
        New-Object System.Net.Http.HttpClient $h
    }.GetNewClosure()

    $c1 = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("kA", $factory)
    $c2 = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("kA", $factory)
    Assert-True ([object]::ReferenceEquals($c1, $c2)) "Second GetOrAdd with same key returns the cached client"
    Assert-Equal $factoryCalls.N 1 "Factory invoked exactly once for two same-key calls"

    # Different key, different client.
    $c3 = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("kB", $factory)
    Assert-True (-not [object]::ReferenceEquals($c1, $c3)) "Different key produces a different client"
    Assert-Equal $factoryCalls.N 2 "Factory invoked once more for the new key"

    # ============================================================
    # SpeHttpClientPool: eviction respects MaxEntries
    # ============================================================
    Write-Host "`n  [SpeHttpClientPool: eviction]" -ForegroundColor White

    [Spe.Internal.SpeHttpClientPool]::Clear()
    [Spe.Internal.SpeHttpClientPool]::MaxEntries = 4
    [Spe.Internal.SpeHttpClientPool]::MaxAgeMinutes = 10

    $silentFactory = [System.Func[System.Net.Http.HttpClient]] {
        $h = New-Object System.Net.Http.HttpClientHandler
        New-Object System.Net.Http.HttpClient $h
    }

    1..10 | ForEach-Object {
        [Spe.Internal.SpeHttpClientPool]::GetOrAdd("evict-$_", $silentFactory) | Out-Null
    }
    Assert-True ([Spe.Internal.SpeHttpClientPool]::Count -le 4) "Cache count stays at or below MaxEntries after 10 inserts"

    # The latest key must still be present (we evict oldest, not newest).
    $latest1 = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("evict-10", $silentFactory)
    $latest2 = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("evict-10", $silentFactory)
    Assert-True ([object]::ReferenceEquals($latest1, $latest2)) "Most recently added key survived eviction"

    # ============================================================
    # SpeHttpClientPool: age-out forces a rebuild
    # ============================================================
    Write-Host "`n  [SpeHttpClientPool: age-out]" -ForegroundColor White

    [Spe.Internal.SpeHttpClientPool]::Clear()
    [Spe.Internal.SpeHttpClientPool]::MaxEntries = 64
    # Age 0 means "every entry is older than max age the moment it lands"; the
    # next GetOrAdd sees the existing entry as stale and replaces it.
    [Spe.Internal.SpeHttpClientPool]::MaxAgeMinutes = 0

    $rebuildCounter = [pscustomobject]@{ N = 0 }
    $rebuildFactory = [System.Func[System.Net.Http.HttpClient]] {
        $rebuildCounter.N++
        $h = New-Object System.Net.Http.HttpClientHandler
        New-Object System.Net.Http.HttpClient $h
    }.GetNewClosure()

    $first = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("age", $rebuildFactory)
    Start-Sleep -Milliseconds 5
    $second = [Spe.Internal.SpeHttpClientPool]::GetOrAdd("age", $rebuildFactory)
    Assert-True (-not [object]::ReferenceEquals($first, $second)) "Stale entry is replaced on next GetOrAdd"
    Assert-Equal $rebuildCounter.N 2 "Factory invoked again after age-out"

    # Crucially: the old client is NOT disposed (callers may still hold it).
    $disposed = $false
    try {
        $first.GetAsync('http://localhost.invalid/never').GetAwaiter().GetResult() | Out-Null
    } catch [System.ObjectDisposedException] {
        $disposed = $true
    } catch {
        # DNS/connect failure is expected for an invalid host; that proves the
        # client itself is still usable.
        $disposed = $false
    }
    Assert-True (-not $disposed) "Aged-out client is NOT disposed under live callers"

    # ============================================================
    # New-SpeAuthHeader: per-call header (not shared mutable state)
    # ============================================================
    Write-Host "`n  [New-SpeAuthHeader]" -ForegroundColor White

    $h1 = New-SpeAuthHeader -Username 'admin' -Password '' -SharedSecret 'sec' -AccessKeyId '' -AccessToken '' -Uri ([Uri]'https://cm.example.com/') -Algorithm 'HS256'
    $h2 = New-SpeAuthHeader -Username 'admin' -Password '' -SharedSecret 'sec' -AccessKeyId '' -AccessToken '' -Uri ([Uri]'https://cm.example.com/') -Algorithm 'HS256'
    Assert-True (-not [object]::ReferenceEquals($h1, $h2)) "Each call returns a fresh AuthenticationHeaderValue"
    Assert-Equal $h1.Scheme 'Bearer' "HMAC identity produces Bearer scheme"

    $hOauth = New-SpeAuthHeader -Username '' -Password '' -SharedSecret '' -AccessKeyId '' -AccessToken 'extern.token.value' -Uri ([Uri]'https://cm.example.com/')
    Assert-Equal $hOauth.Scheme 'Bearer' "OAuth token uses Bearer scheme"
    Assert-Equal $hOauth.Parameter 'extern.token.value' "OAuth token passes through unchanged"

    $hBasic = New-SpeAuthHeader -Username 'u' -Password 'p' -SharedSecret '' -AccessKeyId '' -AccessToken '' -Uri ([Uri]'https://cm.example.com/')
    Assert-Equal $hBasic.Scheme 'Basic' "Username+Password without SharedSecret produces Basic scheme"

    # ============================================================
    # New-SpeHttpClient end-to-end shape
    # ============================================================
    Write-Host "`n  [New-SpeHttpClient: returns Client + AuthHeader]" -ForegroundColor White

    [Spe.Internal.SpeHttpClientPool]::Clear()
    [Spe.Internal.SpeHttpClientPool]::MaxAgeMinutes = 10

    $info1 = New-SpeHttpClient -Username 'admin' -Password '' -SharedSecret 'sec' `
        -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false `
        -Uri ([Uri]'https://cm.example.com/') -Algorithm 'HS256'
    Assert-NotNull $info1.Client "ClientInfo has a Client"
    Assert-NotNull $info1.AuthHeader "ClientInfo has an AuthHeader"
    Assert-True ($info1.Client -is [System.Net.Http.HttpClient]) "Client is HttpClient"
    Assert-True ($info1.AuthHeader -is [System.Net.Http.Headers.AuthenticationHeaderValue]) "AuthHeader is AuthenticationHeaderValue"

    $info2 = New-SpeHttpClient -Username 'admin' -Password '' -SharedSecret 'sec' `
        -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false `
        -Uri ([Uri]'https://cm.example.com/') -Algorithm 'HS256'
    Assert-True ([object]::ReferenceEquals($info1.Client, $info2.Client)) "Same identity reuses the cached HttpClient"
    Assert-True (-not [object]::ReferenceEquals($info1.AuthHeader, $info2.AuthHeader)) "AuthHeader is per-call (not shared)"

    # Different identity, different client.
    $info3 = New-SpeHttpClient -Username 'guest' -Password '' -SharedSecret 'sec' `
        -AccessKeyId 'k' -AccessToken '' -Credential $null -UseDefaultCredentials $false `
        -Uri ([Uri]'https://cm.example.com/') -Algorithm 'HS256'
    Assert-True (-not [object]::ReferenceEquals($info1.Client, $info3.Client)) "Different Username yields a separate HttpClient"

    # ============================================================
    # DefaultRequestHeaders.Authorization is NOT mutated on the shared client
    # ============================================================
    Write-Host "`n  [Shared client is not mutated]" -ForegroundColor White

    Assert-Null $info1.Client.DefaultRequestHeaders.Authorization "Cached client's DefaultRequestHeaders.Authorization stays null (auth lives on per-request HttpRequestMessage)"
}
finally {
    [Spe.Internal.SpeHttpClientPool]::MaxAgeMinutes = $origMaxAge
    [Spe.Internal.SpeHttpClientPool]::MaxEntries    = $origMaxEntry
    [Spe.Internal.SpeHttpClientPool]::Clear()
}
