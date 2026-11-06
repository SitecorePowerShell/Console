# Shared audit-log helpers for integration tests that need to assert on
# server-side audit output. Used by tests that grep SPE.log via docker exec
# (Remoting.GzipBody, Remoting.WaitEventWake, Remoting.AuditSanitization).

function Get-SpeAuditTail {
    # Returns every line of the most recently written SPE.log.*.txt inside the
    # spe-cm-1 container. Caller filters to the audit lines they care about.
    docker exec spe-cm-1 powershell -NoProfile -Command "Get-ChildItem C:/inetpub/wwwroot/App_Data/logs/SPE.log.*.txt | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | ForEach-Object { Get-Content `$_.FullName }"
}

function Find-AuditLine {
    # Convenience: pick the first audit line matching $Action and (optionally)
    # $Correlator. $Correlator is a substring used to scope the search to one
    # request's worth of audit output (typically rid=, sessionId=, or a
    # deliberately-unique ip placeholder).
    param(
        [Parameter(Mandatory)] [string[]] $Lines,
        [Parameter(Mandatory)] [string] $Action,
        [string] $Correlator
    )
    $candidates = $Lines | Where-Object { $_ -match "action=$Action(\s|$)" }
    if ($Correlator) {
        $candidates = $candidates | Where-Object { $_ -match [regex]::Escape($Correlator) }
    }
    return ($candidates | Select-Object -First 1)
}

function Assert-AuditFieldSanitized {
    # Asserts that the named field in $Line carries the URL-encoded form of an
    # injected payload. Sanitized form must contain at least one of %20, %3D
    # (= encoded), or %0A; raw form must NOT appear after the field= delimiter.
    param(
        [Parameter(Mandatory)] [string] $Line,
        [Parameter(Mandatory)] [string] $Field,
        [Parameter(Mandatory)] [string] $RawAttackSubstring,
        [Parameter(Mandatory)] [string] $TestName
    )
    if (-not $Line) {
        Assert-True $false "${TestName}: audit line not found"
        return
    }

    # The raw attacker substring (e.g. " action=loginSuccess") must not appear
    # verbatim. After SanitizeValue, every space and = is URL-encoded so the
    # substring becomes literal-impossible.
    Assert-True ($Line -notmatch [regex]::Escape($RawAttackSubstring)) `
        "${TestName}: raw attacker substring '$RawAttackSubstring' is NOT present in audit line"

    # And the sanitized form must be present, attached to the named field.
    Assert-True ($Line -match "$Field=[^\s]*%(20|3D|0A)") `
        "${TestName}: $Field= carries URL-encoded sanitized payload (%20 / %3D / %0A)"
}
