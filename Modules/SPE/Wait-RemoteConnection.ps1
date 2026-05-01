function Wait-RemoteConnection {
    <#
        .SYNOPSIS
            Blocks until a Sitecore instance is ready to accept remoting
            calls. Stronger than Test-RemoteConnection: also exercises the
            Sitecore Jobs dispatcher, not just the auth pipeline.

        .DESCRIPTION
            Right after a CM app-pool recycle (typical of `task deploy` or any
            Sitecore restart) two subsystems warm independently. The auth
            pipeline can briefly reject otherwise-valid JWTs, and the Sitecore
            Jobs dispatcher may take several seconds to schedule the first
            background script even though sessions are already registered.

            Wait-RemoteConnection retries a lightweight Test-RemoteConnection
            probe until auth succeeds, then fires a throw-away Invoke-RemoteScript
            -AsJob and waits for it to complete. The second phase forces the
            Job runner to dispatch its first background script, so the next
            real -AsJob from the caller sees a steady-state container.

            Returns nothing on success. Throws if either phase fails to
            complete within -TimeoutSeconds. Intended for CI harnesses,
            smoke scripts, and any automation that must succeed on the
            first call after a deploy. Use Test-RemoteConnection (which
            shares the noun) for one-shot connectivity checks where you
            only need a boolean.

            The warmup -AsJob session is created against the same caller
            credentials but gets its own job session id and is left to
            expire on its TTL - the caller's session is never modified.

        .PARAMETER Session
            A ScriptSession created by New-ScriptSession.

        .PARAMETER TimeoutSeconds
            Hard ceiling for the entire warmup. Cold containers typically
            settle in 10-60 s; the default 120 s leaves headroom for slow
            machines without hanging a broken deploy indefinitely.

        .EXAMPLE
            $session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
            Wait-RemoteConnection -Session $session
            Invoke-RemoteScript -Session $session -ScriptBlock { Get-Item 'master:/sitecore' } -AsJob

        .LINK
            New-ScriptSession

        .LINK
            Test-RemoteConnection

        .LINK
            Wait-RemoteScriptSession
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateNotNull()]
        [pscustomobject]$Session,

        [Parameter()]
        [ValidateRange(5, 600)]
        [int]$TimeoutSeconds = 120
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)

    # The warmup runs both phases (cheap auth probe + tiny -AsJob round
    # trip) inside a single retry loop. Cold containers can flap between
    # "test endpoint answers" and "script endpoint still warming"; if the
    # AsJob fails even though Test-RemoteConnection just returned true,
    # we just sleep and try the whole sequence again rather than declare
    # failure. Inner errors are swallowed via -ErrorAction / 2>$null so
    # the host is not flooded with retry stack traces; the final throw
    # below is the single user-visible failure if the deadline lapses.
    $authEverPassed   = $false
    $jobEverDispatched = $false
    while ((Get-Date) -lt $deadline) {
        # Phase 1: cheap auth probe. Test-RemoteConnection swallows its
        # own HTTP exceptions and returns $false on auth failure.
        $testOk = $false
        try { $testOk = [bool](Test-RemoteConnection -Session $Session -Quiet) } catch { }
        if (-not $testOk) {
            Start-Sleep -Seconds 2
            continue
        }
        $authEverPassed = $true
        Write-Verbose "Wait-RemoteConnection: auth probe succeeded."

        # Phase 2: throw-away -AsJob round-trip. Errors here (auth flap,
        # session-not-yet-registered, brief 503s during warmup) are
        # silenced; we just retry on the next loop iteration.
        $jobId = $null
        try {
            $jobId = Invoke-RemoteScript -Session $Session -ScriptBlock { "warmup-ok" } `
                -AsJob -Raw -ErrorAction SilentlyContinue 2>$null
        } catch { }
        if ([string]::IsNullOrEmpty($jobId)) {
            Start-Sleep -Seconds 2
            continue
        }
        $jobEverDispatched = $true
        Write-Verbose "Wait-RemoteConnection: -AsJob dispatched ($jobId), waiting for completion."

        $remaining = [int][Math]::Max(15, ($deadline - (Get-Date)).TotalSeconds)
        $output = $null
        try {
            $output = Wait-RemoteScriptSession -Session $Session -Id $jobId `
                -WaitTimeoutSeconds $remaining -ErrorAction SilentlyContinue 2>$null
        } catch { }
        if ([string]$output -match "warmup-ok") {
            Write-Verbose "Wait-RemoteConnection: background probe succeeded."
            return
        }
        Start-Sleep -Seconds 2
    }

    # Deadline lapsed. Surface the most informative diagnostic we can:
    # the symptom maps to one of three operator-actionable causes.
    if (-not $authEverPassed) {
        throw "Wait-RemoteConnection: auth probe never succeeded within ${TimeoutSeconds}s. CM may not be running, the shared secret/credential may be wrong, or the remoting service may be disabled."
    }
    if (-not $jobEverDispatched) {
        throw "Wait-RemoteConnection: auth succeeded but -AsJob calls kept failing within ${TimeoutSeconds}s. Check the SPE log for policy or service-enabled errors."
    }
    throw "Wait-RemoteConnection: -AsJob dispatched but never completed within ${TimeoutSeconds}s. The Sitecore Jobs dispatcher may be stalled."
}
