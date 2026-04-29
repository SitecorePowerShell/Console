# Streaming Progress from a Remote Script

A `Wait-RemoteScriptSession` waiting on an `-AsJob` script normally returns
nothing until the script ends - the runspace's Output stream is buffered and
only released to `Receive-ScriptSession` once the runspace is no longer Busy.
For long-running jobs (publish, indexing, content migrations) that produces a
silent CI log for minutes at a time and zero feedback for the operator who
kicked it off.

The wait endpoint can also tee `Verbose`, `Information`, `Progress`, and
`Warning` records out of the running runspace and stream them back during the
long-poll. This page describes how to use it from the SPE PowerShell module
and what to know about the bounds, the audit trail, and the policy
interaction.

## Quickstart

```powershell
$session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore

$jobId = Invoke-RemoteScript -Session $session -ScriptBlock {
    1..100 | ForEach-Object {
        Write-Verbose "Processing item $_"           -Verbose
        Write-Progress -Activity "Indexing" -Status "$_ of 100" -PercentComplete $_
        # ... real work ...
    }
    "Indexed 100 items"
} -AsJob

Wait-RemoteScriptSession -Session $session -Id $jobId `
    -OnVerbose  { param($r) Write-Host "[v] $($r.Message)" } `
    -OnProgress { param($r) Write-Host "[p] $($r.Activity) - $($r.PercentComplete)%" }

Stop-ScriptSession -Session $session
```

The two scriptblocks fire as records arrive, not at the end. `Output` (the
script's return value) still drains via `Receive-ScriptSession` once the
runspace finishes - that's what `Wait-RemoteScriptSession` returns when the
loop exits.

## The four streams

| Parameter        | Source cmdlet      | Record fields                                                                                                                  |
| ---------------- | ------------------ | ------------------------------------------------------------------------------------------------------------------------------ |
| `-OnVerbose`     | `Write-Verbose`    | `Stream`, `Sequence`, `TimeUtc`, `Message`                                                                                     |
| `-OnInformation` | `Write-Information`, `Write-Host` | `Stream`, `Sequence`, `TimeUtc`, `Message`                                                                  |
| `-OnWarning`     | `Write-Warning`    | `Stream`, `Sequence`, `TimeUtc`, `Message`                                                                                     |
| `-OnProgress`    | `Write-Progress`   | `Stream`, `Sequence`, `TimeUtc`, `Activity`, `StatusDescription`, `PercentComplete`, `CurrentOperation`, `SecondsRemaining`, `ParentActivityId`, `RecordType` |

Records are unified-ordered: `Sequence` is monotonic across all four streams
combined, so an interleaved log view ("step 3 verbose, then 30% progress,
then step 4 verbose") reflects emission order on the server.

`Write-Host` shares `-OnInformation` because PowerShell 5.0+ routes it
through the Information stream under the hood. The record's `Message`
carries the text but loses any color hints (`ForegroundColor`,
`BackgroundColor`) that the host-side `Write-Host` would have applied.

## Stream-to-cmdlet reference (and what does not stream)

| Producer in the remote script                                | Stream      | Subscriber       |
| ------------------------------------------------------------ | ----------- | ---------------- |
| `Write-Verbose ... -Verbose`                                 | Verbose     | `-OnVerbose`     |
| `Write-Information ... -InformationAction Continue`          | Information | `-OnInformation` |
| `Write-Host`                                                 | Information | `-OnInformation` |
| `Write-Warning`                                              | Warning     | `-OnWarning`     |
| `Write-Progress`                                             | Progress    | `-OnProgress`    |
| `Write-Output`, bare expressions, implicit return values     | **Output**  | none (drains at end via `Receive-ScriptSession`) |
| `Write-Error`, terminating exceptions                        | Error       | none (read `LastErrors` on the session after wait) |
| `Write-Debug`                                                | Debug       | none (intentionally excluded; use Verbose for diagnostic streaming) |

### Common gotcha: `Write-Output` does not stream

A subscriber on `-OnInformation` does **not** see `Write-Output` calls. Output
is the runspace's return-value pipeline, gated by the Busy-state guard in
`Receive-ScriptSession` for concurrency safety. If your script does:

```powershell
1..6 | ForEach-Object {
    Write-Output "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))"
    Start-Sleep -Seconds 2
}
```

the six lines and any subsequent return value sit in the buffer until the
runspace goes Idle, then arrive in one batch via the wait cmdlet's final
drain. The `-On*` callbacks never fire.

To stream the same lines live, use `Write-Information`, `Write-Verbose`, or
`Write-Host` and subscribe to the matching `-On*` parameter. If you also need
the same content in the script's return value, emit it twice:

```powershell
1..6 | ForEach-Object {
    $line = "Step $_ at $([DateTime]::Now.ToString('HH:mm:ss'))"
    Write-Information $line -InformationAction Continue   # streams live
    $line                                                 # to Output, returned at end
    Start-Sleep -Seconds 2
}
```

## Behavior under hardened policies

The four `Write-*` cmdlets and `Write-Progress` are part of the SPE
`StreamBaseline` (`src/Spe/Core/Settings/Authorization/RemotingPolicy.cs`),
which means they are implicitly allowed under any Remoting Policy regardless
of the policy's `PolicyAllowedCmds` list. **The streaming feature is
therefore not gated by the policy's command allowlist.** This is the
deliberate design - those cmdlets are I/O primitives, not behavior - but
operators upgrading from a pre-feature SPE should be aware of the change in
observability:

- Pre-feature: `Write-Verbose` from a remoting script emitted into the
  runspace's verbose stream and was unobservable to the caller.
- Post-feature: the same `Write-Verbose` is observable to any caller passing
  `-OnVerbose` and the corresponding cursor.

No policy capability changed - any data the script could read (and put into a
verbose message) it could already exfiltrate via the synchronous `Output`
path. But operators who used the cmdlet allowlist as a proxy for "what the
caller can see" should explicitly review what their scripts log.

A per-policy `AllowProgressStreams` toggle is intentionally not part of v1.
If a deployment needs to suppress the channel, raise an issue.

## Bounds

The per-session ring buffer enforces three caps:

| Cap            | Value                  | Behavior on overflow                                                  |
| -------------- | ---------------------- | --------------------------------------------------------------------- |
| Per-record     | 4 KB (characters)      | Truncated, suffixed with `[...truncated]`                             |
| Per-session    | ~1 MB approx           | Drop-oldest until the new record fits; `droppedCount` increments     |
| Rate           | 100 records / second   | Excess writes dropped immediately; `droppedCount` increments          |

The client receives `droppedCount` on every wait response. When it
increases, `Wait-RemoteScriptSession` emits a yellow-bar warning:

```
WARNING: Stream buffer for job <id> dropped <N> record(s) due to rate or size cap.
```

If you see this in CI, your script is emitting faster than 100 records/sec
or producing very large records. Either way, reduce the emit rate - the
caller's view of progress is already lossy and the script is paying lock
contention on the ring.

## How the cursor works

The wait endpoint accepts an opaque `cursor` query-string parameter that
encodes the session id and the offset into the per-session ring. Cursors are
HMAC-SHA256 signed with a per-app-domain key - they cannot be forged, and a
cursor minted for session A cannot be replayed against session B.

`Wait-RemoteScriptSession` manages the cursor automatically - clients of the
PowerShell module should never see it. Direct HTTP callers (see
`remoting-raw-http.md`) pass `cursor=` (empty string) on the first call and
echo back the `cursor` field from the response on subsequent calls.

The signing key is regenerated on every app-pool start, so cursors do not
survive a recycle. That is fine: the records they pointed at and the script
session that produced them die in the same recycle, so any post-recycle wait
on an old cursor would resolve to `NotFound` regardless.

## Audit trail

When records are drained on a wait response, an audit line is emitted at
Standard+ audit levels:

```
INFO  AUDIT [Remoting] action=progressStreamed session=<id> count=<N> dropped=<M> ip=<addr> rid=<rid>
```

Operators auditing what flows through the channel can grep for
`action=progressStreamed`. The records themselves are never logged - only
metadata. To audit content, add `Sitecore.Diagnostics.Log` calls inside the
script and read them via the standard log inspection tools.

## What it does not do

- Surface `Output` mid-flight. That is `Receive-ScriptSession`'s job and runs
  after the runspace goes Idle.
- Stream `Sitecore.Jobs.Job` progress (publish, index rebuild). Those run
  outside a PowerShell runspace; the four streams do not exist there. Use
  `Wait-RemoteSitecoreJob` and inspect `Status.Messages` for native job
  progress.
- Survive an app-pool recycle. Records, sessions, and cursors all die
  together; the next wait sees `NotFound`.
- Stream `Debug` records. The intent is operator-visible progress, not
  developer logging.

## Backwards compatibility

- Old client + new server: the client doesn't pass `-OnVerbose` etc., the
  cmdlet doesn't request streams, the server doesn't include `streams` /
  `cursor` in the response, behavior is identical to pre-feature.
- New client + old server: the cmdlet requests streams (`cursor=` empty),
  the old server ignores the unknown query param, the response has no
  `streams` field, the cmdlet sees zero records and just returns the
  Output drain at the end. The user-visible result is silence on the
  scriptblock callbacks, which is honest given the server's capability.

## Wire shape

For HTTP callers and tooling implementers, the response from
`/-/script/wait/` looks like:

```json
{
  "isDone": false,
  "status": "Busy",
  "name": "<job-id>",
  "elapsedSeconds": 12,
  "streams": [
    { "stream": "verbose",  "sequence": 0, "timeUtc": "2026-04-29T13:00:00.000Z", "message": "Processing item 1" },
    { "stream": "progress", "sequence": 1, "timeUtc": "2026-04-29T13:00:00.001Z",
      "activity": "Indexing", "statusDescription": "1 of 100", "percentComplete": 1,
      "currentOperation": "", "secondsRemaining": -1, "parentActivityId": -1, "recordType": "Processing" }
  ],
  "cursor": "<opaque>",
  "droppedCount": 0
}
```

`streams`, `cursor`, and `droppedCount` are present only when the caller
passed a `cursor` query param. Without that, the response shape is identical
to pre-feature.
