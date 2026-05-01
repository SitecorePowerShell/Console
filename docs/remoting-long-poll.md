# Remoting Long-Poll Wait

`Wait-RemoteScriptSession` and `Wait-RemoteSitecoreJob` use a long-poll
endpoint at `/-/script/wait/` to detect job completion. Compared to the
legacy "client sleeps, then re-asks" pattern they replaced, long-poll
returns sooner, fires far fewer requests, and (when used together with
[progress streams](remoting-progress-streams.md)) delivers stream records
within ~200 ms of the script writing them.

This page is a mechanism explainer. The wire-level API reference lives in
[`remoting-raw-http.md`](remoting-raw-http.md); the original design proposal
is GitHub issue #1474.

## The two modes side by side

Suppose a remote `-AsJob` script finishes at `t=4.7s` and the operator runs
`Wait-RemoteScriptSession` with `-Delay 1` (the legacy default).

### Legacy poll: client wakes up every `Delay` seconds

```
  time -> 0s    1s    2s    3s    4s    5s
          |     |     |     |     |     |
client:  [R]---[R]---[R]---[R]---[R]---[R]
          ^script ran                    ^
          ^script done at 4.7            client doesn't notice until 5.0
                                         6 round trips (auth + JWT + parse)
                                         detection lag up to `Delay` seconds
```

Each `[R]` is an `Invoke-RemoteScript` call shipping a "is this session
busy?" scriptblock to the server. Every call mints a JWT, walks the
authentication pipeline, runs the policy scan, and burns one request from
the API key's throttle budget.

### Long-poll: server holds the call

```
  time -> 0s . . . . . . . . . . . . . 4.7s  4.9s
          |                              |     |
client:  [R============held by server====|=====R]
server:   . . . . . . . . . . . . . . . .^
            internal 200 ms ticks         sees isDone, returns
                                          1 round trip
                                          detection lag bounded by ~200 ms
```

The client opens one HTTP request and the server's async handler
(`ProcessWaitAsync` in `RemoteScriptCall.ashx.cs`) parks on a 200 ms loop
that probes job state in-process. The instant `isDone` flips, the loop
breaks and the response returns.

## Why it is fast

Four mechanisms stack:

1. **Detection lag drops from `Delay` to ~200 ms.** Legacy is bounded by
   the client sleep, which the operator sets to keep request pressure
   reasonable. Long-poll is bounded by how often the server itself checks
   state. The server can poll at 200 ms because it is reading in-process
   memory: no auth pipeline, no TLS, no throttle bookkeeping per check.

2. **Stream records short-circuit the held call.** When the script writes
   a `Verbose` / `Information` / `Progress` / `Warning` record, the next
   200 ms tick sees it in the per-session ring, breaks out of the loop,
   and returns *now* with the record batch and a fresh signed cursor. The
   client immediately re-issues with the new cursor. Result: live progress
   over plain request/response HTTP, no SSE or WebSocket.

   ```
   Script emits records at t=1.2, t=3.0, t=3.8, then finishes at t=4.7.

     time ->  0      1.2     3.0    3.8    4.7
              |       |       |      |      |
   client:   [R~~~~~~~R][R~~~~R][R~~~R][R~~~R]
                     ^     ^      ^       ^
                 Verbose Progress Verbose isDone
   ```

3. **Auth and TLS overhead is amortized.** Legacy fires `job-duration / Delay`
   requests. Long-poll fires roughly `job-duration / TimeoutSeconds` requests
   plus one per stream record batch. A 5-minute silent job is ~300 round
   trips legacy vs. ~10 long-poll. The `HttpClient` is also cached per
   connection URI in `Expand-ScriptSession`, so TCP/TLS connection reuse
   kicks in across the few requests that do go out.

4. **No client-side sleep.** The legacy loop calls `Start-Sleep -Seconds $Delay`
   between polls. Long-poll has no client sleep at all - the held call *is*
   the wait. The legacy fallback path (`useLongPoll = $false` after a 404
   from an old server) re-introduces the sleep because old servers do not
   have anything to hold against.

## Bounds and tuning knobs

| Knob                      | Default | Range  | What it controls                                                    |
| ------------------------- | ------- | ------ | ------------------------------------------------------------------- |
| `-WaitTimeoutSeconds`     | 30      | 1..60  | Max time the server holds one call before returning current state. |
| `-Delay`                  | 1       | n/a    | Sleep between iterations on the legacy fallback path *only*.        |
| `-MaxRetries`             | 2       | 0..10  | Retries on 429/503 from either path before giving up.               |

`-WaitTimeoutSeconds` is clamped server-side to 1..60. Pushing it higher
on the client has no effect; the response simply returns at 60. The
default of 30 trades request count against IIS request-timeout headroom -
each held call occupies a request slot but no thread (the handler is
async).

`-Delay` only matters when the server returns 404 on `/-/script/wait/`
(pre-9.0 server) and the client falls back to the legacy per-poll path.
On a new-on-new deployment, `-Delay` is unused.

## When long-poll falls back to legacy

`Invoke-RemoteWait` returns a `NotSupported = $true` result on HTTP 404
from the wait endpoint. The wait cmdlet flips `$useLongPoll = $false`,
emits one verbose log line, and continues with `Invoke-RemoteScript $doneScript`
plus client-side `Start-Sleep -Seconds $Delay` between iterations.

The fallback is automatic and per-session: the cmdlet does not retry the
wait endpoint after the first 404 in a given wait. Restart the cmdlet to
re-probe.

Transport errors (DNS, connection reset, TLS failure) on the held call
return `Status = 'TransportError'` and the client retries after `Delay`
without falling back - the endpoint may exist and the network may simply
be flaky.

## Server-side behaviour

The handler is `RemoteScriptCall.ashx.cs ProcessWaitAsync`. Key points:

- **Async I/O.** Implements `HttpTaskAsyncHandler`. The held call parks on
  an IOCP completion port via `await Task.Delay(200, cancellationToken)`,
  not a blocked thread. IIS thread-pool pressure is the same as for any
  other request that takes 30 s to return a payload.
- **Client disconnect honoured.** The handler watches
  `context.Response.ClientDisconnectedToken`. Ctrl+C in the client breaks
  out of the server loop on the next 200 ms tick and frees the request.
- **Same auth as every other route.** `AuthenticateRequest` runs once per
  call. Session ownership is checked too - sessions created by identity A
  cannot be waited on by identity B; mismatch returns 403 with
  `X-SPE-Restriction: session-not-owned`.
- **Throttle accounting.** One request against the API key's budget per
  held call regardless of hold time. A long job that takes 30 minutes
  costs ~60 budget units long-poll, ~1800 legacy.
- **Uniform NotFound response.** Unknown `jobId` returns 200 with
  `{"status":"NotFound","isDone":true}` rather than 404, so callers
  cannot enumerate session ids by probing.
- **Pending vs Idle.** A script-session whose runspace is `Available`
  may either be pending (Sitecore Jobs has not scheduled `RunJob` yet)
  or genuinely finished. The handler tracks a one-way "has the run
  started" latch on the session and reports `Pending` until the latch
  flips. `isDone` is true only for `Idle`, so a wait against a freshly
  created `-AsJob` session never short-circuits before the script has
  actually started.

## Cold-start considerations

Right after `task deploy` (or any CM app-pool recycle) two subsystems
warm independently:

1. **The auth pipeline.** The first one or two remoting calls can
   return `Authentication failed` while JWT validation, key loading,
   and the policy scan settle. Subsequent calls succeed.
2. **The Sitecore Jobs dispatcher.** `Invoke-RemoteScript -AsJob`
   returns a session id immediately, but the job runner may take a
   few seconds to schedule and start `RunJob`. Within that window the
   wait endpoint reports `Pending` (see above) so the client does not
   falsely conclude the script is done - but if the wait's
   `WaitTimeoutSeconds` is short and the dispatcher is very cold, the
   held call can deadline before the script even starts.

For test harnesses and CI smokes, run a one-shot warmup probe before
the first real call so the cold-container cost lands somewhere
predictable. The SPE Remoting module ships `Wait-RemoteConnection` for
exactly this. It pairs with `Test-RemoteConnection` - same noun, but
where `Test-` returns a boolean for fast health checks, `Wait-` blocks
until the instance is fully ready and throws on timeout:

```powershell
$session = New-ScriptSession -Username admin -SharedSecret $secret -ConnectionUri https://remotesitecore
Wait-RemoteConnection -Session $session

# Now safe to fire the first real -AsJob.
Invoke-RemoteScript -Session $session -ScriptBlock { ... } -AsJob
```

The cmdlet retries `Test-RemoteConnection` until auth succeeds, then
fires a tiny `-AsJob` script and waits for it to complete - which
forces the Job runner to dispatch its first background script.
After it returns, the next `-AsJob` from the same harness will see
a warm dispatcher.

Operators running ad-hoc scripts can usually skip the warmup; one
or two retries on the first call suffice. Automated harnesses that
must succeed on the first attempt should always warm.

## What long-poll does not do

- **It is not push.** The client still drives the conversation; the server
  just holds the response longer than usual. There is no server-initiated
  callback channel.
- **It does not survive app-pool recycles.** Sessions, records, and
  cursors are in-memory and app-domain-scoped. A recycle drops them all,
  and the next wait against a recycled session returns `NotFound`. This
  is fine in practice because the runspace and any scripted progress
  toward completion die in the same recycle.
- **It does not surface `Output` mid-flight.** That is
  `Receive-ScriptSession`'s job and runs after the runspace goes Idle.
  See [progress streams](remoting-progress-streams.md) for what does
  surface mid-flight.
