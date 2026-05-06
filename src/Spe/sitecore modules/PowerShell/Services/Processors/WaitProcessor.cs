using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Host;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class WaitProcessor
    {
        // GET /-/script/wait/ long-poll endpoint. Holds the request until the
        // target job (or script session) transitions to done, or until the
        // caller-provided timeoutSeconds (clamped 1..60) elapses. Returns
        // JSON so clients can read status without a PS runspace. The
        // endpoint is throttled, authenticated, and access-controlled via
        // the same AuthenticateRequest pipeline as every other route.
        public static async Task ProcessWaitAsync(HttpContext context)
        {
            var request = context.Request;
            var serviceName = WebServiceSettings.ServiceRemoting;
            var origin = request.Headers["Origin"];

            // Verbose-only: duplicates action=requestReceived emitted at the
            // top of ProcessRequestAsync, and fires once per stream record on
            // fast streamers (~20/sec). Gated at Full so default Standard
            // policies do not carry the per-record line; operators explicitly
            // tracing remoting traffic raise to Full to opt in.
            if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Full)
            {
                PowerShellLog.Audit($"[Remoting] action=waitRequestReceived service={serviceName} ip={RemotingRequestContext.GetIp(request)} rid={RemotingRequestContext.GetRequestId()}");
            }

            if (!AuthProcessor.CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthProcessor.AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }
            if (!isAuthenticated || identity == null)
            {
                AuthProcessor.RejectAuthenticationMethod(context, serviceName, null);
                return;
            }

            var sessionId = request.QueryString["sessionId"];
            var jobId = request.QueryString["jobId"];
            var jobType = request.QueryString["jobType"] ?? "scriptsession";
            var timeoutSecondsRaw = request.QueryString["timeoutSeconds"];
            var cursorParam = request.QueryString["cursor"];

            if (string.IsNullOrEmpty(jobId))
            {
                ErrorResponseWriter.SetErrorResponse(context, 400, "jobId is required.");
                return;
            }

            // Cursor (optional): opaque, HMAC-signed by the server. Encodes the
            // session id and the offset into the per-session StreamRing where the
            // caller wants to resume. Missing cursor = read from offset 0.
            // Tampered or stale-key cursor = 400 invalid_cursor.
            long fromSequence = 0;
            if (!string.IsNullOrEmpty(cursorParam))
            {
                if (!CursorSigner.TryVerify(cursorParam, jobId, out fromSequence))
                {
                    ErrorResponseWriter.SetErrorResponse(context, 400, "invalid_cursor");
                    return;
                }
            }

            int timeoutSeconds = 30;
            if (!string.IsNullOrEmpty(timeoutSecondsRaw) && int.TryParse(timeoutSecondsRaw, out var parsed))
            {
                timeoutSeconds = parsed;
            }
            if (timeoutSeconds < 1) timeoutSeconds = 1;
            if (timeoutSeconds > 60) timeoutSeconds = 60;

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            var cancellationToken = context.Response.ClientDisconnectedToken;
            var started = DateTime.UtcNow;

            // After an app-pool recycle, an Invoke-RemoteScript -AsJob can
            // return its job id before ScriptSessionManager / HttpRuntime.Cache
            // make the session visible to GetMatchingSessionsForAnyUserSession.
            // A wait fired immediately after that AsJob would otherwise see
            // NotFound and falsely report isDone=true. Treat NotFound as
            // "registration race, retry" only for the first NotFoundGraceMs of
            // the held call - after that, NotFound is genuine.
            const int NotFoundGraceMs = 750;

            string status = "NotFound";
            string name = jobId;
            bool isDone = true;

            ScriptSession targetSession = null;
            StreamSnapshot streams = null;

            while (DateTime.UtcNow < deadline)
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (string.Equals(jobType, "sitecore", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryProbeSitecoreJob(jobId, out status, out name, out isDone))
                    {
                        if (isDone) break;
                    }
                    else if ((DateTime.UtcNow - started).TotalMilliseconds < NotFoundGraceMs)
                    {
                        // Registration race - tick and retry the lookup.
                    }
                    else
                    {
                        // Unknown handle - uniform response (no 404) to resist enumeration.
                        status = "NotFound";
                        isDone = true;
                        break;
                    }
                }
                else if (string.Equals(jobType, "scriptsession", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryProbeScriptSession(context, sessionId, jobId, out status, out name, out isDone, out var ownershipRejected, out targetSession))
                    {
                        if (ownershipRejected) return;

                        // Drain any new stream records the script has emitted since
                        // the caller's cursor. New records short-circuit the wait
                        // (don't hold the request to the deadline) so progress feels
                        // real-time on the client. ReadFrom is cursor-based and
                        // non-destructive, so two concurrent waiters on the same
                        // sessionId (e.g. a retry overlapping the original wait)
                        // both observe the same records via their own fromSequence
                        // offsets - intentional, no waiter exclusion needed.
                        if (targetSession != null)
                        {
                            var snap = targetSession.StreamRing.ReadFrom(fromSequence, 256);
                            if (snap.Records.Count > 0)
                            {
                                streams = snap;
                                break;
                            }
                        }

                        if (isDone) break;
                    }
                    else if ((DateTime.UtcNow - started).TotalMilliseconds < NotFoundGraceMs)
                    {
                        // Registration race - tick and retry the lookup.
                    }
                    else
                    {
                        status = "NotFound";
                        isDone = true;
                        break;
                    }
                }
                else
                {
                    ErrorResponseWriter.SetErrorResponse(context, 400, "Unsupported jobType. Expected 'scriptsession' or 'sitecore'.");
                    return;
                }

                var remaining = deadline - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero) break;

                try
                {
                    if (targetSession != null)
                    {
                        // Event-driven wake: returns when the session signals
                        // an update (stream-record enqueue, runspace state /
                        // availability change, or disposal) or when the
                        // remaining deadline elapses, whichever comes first.
                        await targetSession.WaitForUpdateAsync(remaining, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        // No session bound yet (registration-race grace window
                        // for jobType=scriptsession) or jobType=sitecore (no
                        // per-session signal source). 200ms tick so the
                        // NotFoundGraceMs retry path continues to fire.
                        await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                    }
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            // If the loop exited via isDone or deadline (not via streams short-circuit)
            // and we have a script session, do a final read so the caller gets any
            // records emitted between the last loop iteration and the exit.
            if (streams == null && targetSession != null)
            {
                var snap = targetSession.StreamRing.ReadFrom(fromSequence, 256);
                if (snap.ProducedSoFar > 0)
                {
                    streams = snap;
                }
            }

            var elapsed = (int)Math.Round((DateTime.UtcNow - started).TotalSeconds);

            // Cumulative ring counters reflect session-lifetime totals on the
            // StreamRing; emit them on every response (including NotFound and
            // empty polls) so a monitoring client sees a continuous series
            // and a quiet poll between busy ones doesn't drop the value to
            // undefined. Default 0 when no snapshot was produced (NotFound,
            // or session has had no production yet).
            long droppedCount   = streams?.TotalDropped   ?? 0L;
            long droppedRate    = streams?.DroppedRate    ?? 0L;
            long droppedSize    = streams?.DroppedSize    ?? 0L;
            long truncatedCount = streams?.TruncatedCount ?? 0L;

            var sb = new StringBuilder(256);
            sb.Append("{\"isDone\":").Append(isDone ? "true" : "false");
            sb.Append(",\"status\":\"").Append(ErrorResponseWriter.JsonEscape(status)).Append("\"");
            sb.Append(",\"name\":\"").Append(ErrorResponseWriter.JsonEscape(name ?? jobId)).Append("\"");
            sb.Append(",\"elapsedSeconds\":").Append(elapsed);
            // droppedCount is the sum (back-compat for callers minted pre-split).
            // droppedRate / droppedSize / truncatedCount let new clients tell the
            // operator which cap is firing.
            sb.Append(",\"droppedCount\":").Append(droppedCount);
            sb.Append(",\"droppedRate\":").Append(droppedRate);
            sb.Append(",\"droppedSize\":").Append(droppedSize);
            sb.Append(",\"truncatedCount\":").Append(truncatedCount);

            if (streams != null)
            {
                sb.Append(",\"streams\":");
                AppendStreamsJson(sb, streams.Records);
                sb.Append(",\"cursor\":\"").Append(CursorSigner.Sign(jobId, streams.NextOffset)).Append("\"");

                if (streams.Records.Count > 0)
                {
                    int errorCount = 0;
                    for (int i = 0; i < streams.Records.Count; i++)
                    {
                        if (streams.Records[i].Stream == StreamKinds.Error) errorCount++;
                    }
                    var auditPolicy = RemotingRequestContext.GetActivePolicy();
                    // count=1 is the common case for streaming responses, so
                    // this line is gated at Full to keep default Standard
                    // policies quiet. Operators tracing streaming forensics
                    // raise to Full.
                    if (auditPolicy.AuditLevel >= AuditLevel.Full)
                    {
                        PowerShellLog.Audit("[Remoting] action=progressStreamed session={0} count={1} errors={2} dropRate={3} dropSize={4} truncated={5} ip={6} rid={7}",
                            jobId, streams.Records.Count, errorCount, streams.DroppedRate, streams.DroppedSize, streams.TruncatedCount, RemotingRequestContext.GetIp(request), RemotingRequestContext.GetRequestId(context));
                    }
                    // Errors are rare and forensically critical, so they stay
                    // visible at Standard alongside other expected-traffic
                    // audit lines (sessionCleanup, scriptHashLogged, etc.).
                    if (errorCount > 0 && auditPolicy.AuditLevel >= AuditLevel.Standard)
                    {
                        PowerShellLog.Audit("[Remoting] action=errorStreamed session={0} count={1} ip={2} rid={3}",
                            jobId, errorCount, RemotingRequestContext.GetIp(request), RemotingRequestContext.GetRequestId(context));
                    }
                }
            }
            sb.Append("}");

            context.Response.ContentType = "application/json";
            context.Response.Write(sb.ToString());

            HttpResponseHelpers.AddCorsHeaders(context, serviceName, origin);
        }

        private static void AppendStreamsJson(StringBuilder sb, IReadOnlyList<StreamRecord> records)
        {
            sb.Append("[");
            for (int i = 0; i < records.Count; i++)
            {
                if (i > 0) sb.Append(",");
                var r = records[i];
                sb.Append("{\"stream\":\"").Append(r.Stream).Append("\"");
                sb.Append(",\"sequence\":").Append(r.Sequence);
                sb.Append(",\"timeUtc\":\"").Append(r.TimeUtc.ToString("o")).Append("\"");

                if (r.Payload is ProgressPayload p)
                {
                    sb.Append(",\"activity\":\"").Append(ErrorResponseWriter.JsonEscape(p.Activity ?? string.Empty)).Append("\"");
                    sb.Append(",\"statusDescription\":\"").Append(ErrorResponseWriter.JsonEscape(p.Status ?? string.Empty)).Append("\"");
                    sb.Append(",\"percentComplete\":").Append(p.PercentComplete);
                    sb.Append(",\"currentOperation\":\"").Append(ErrorResponseWriter.JsonEscape(p.CurrentOperation ?? string.Empty)).Append("\"");
                    sb.Append(",\"secondsRemaining\":").Append(p.SecondsRemaining);
                    sb.Append(",\"parentActivityId\":").Append(p.ParentActivityId);
                    sb.Append(",\"recordType\":\"").Append(ErrorResponseWriter.JsonEscape(p.RecordType ?? string.Empty)).Append("\"");
                }
                else if (r.Payload is ErrorPayload ep)
                {
                    sb.Append(",\"message\":\"").Append(ErrorResponseWriter.JsonEscape(ep.Message ?? string.Empty)).Append("\"");
                    sb.Append(",\"fullyQualifiedErrorId\":\"").Append(ErrorResponseWriter.JsonEscape(ep.FullyQualifiedErrorId ?? string.Empty)).Append("\"");
                    sb.Append(",\"categoryInfo\":\"").Append(ErrorResponseWriter.JsonEscape(ep.CategoryInfo ?? string.Empty)).Append("\"");
                    sb.Append(",\"positionMessage\":\"").Append(ErrorResponseWriter.JsonEscape(ep.PositionMessage ?? string.Empty)).Append("\"");
                    sb.Append(",\"scriptStackTrace\":\"").Append(ErrorResponseWriter.JsonEscape(ep.ScriptStackTrace ?? string.Empty)).Append("\"");
                }
                else
                {
                    sb.Append(",\"message\":\"").Append(ErrorResponseWriter.JsonEscape((r.Payload as string) ?? string.Empty)).Append("\"");
                }
                sb.Append("}");
            }
            sb.Append("]");
        }

        private static bool TryProbeSitecoreJob(string jobHandle, out string status, out string name, out bool isDone)
        {
            status = "NotFound"; name = jobHandle; isDone = true;
            try
            {
                var handle = Sitecore.Handle.Parse(jobHandle);
                if (handle == null) return false;
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager?.GetJob(handle);
                if (job == null) return false;
                name = job.Name ?? jobHandle;
                isDone = job.IsDone || job.StatusFailed;
                status = job.StatusFailed ? "Failed" : (job.IsDone ? "Done" : "Busy");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryProbeScriptSession(HttpContext context, string sessionId, string jobSessionId, out string status, out string name, out bool isDone, out bool ownershipRejected, out ScriptSession targetSession)
        {
            status = "NotFound"; name = jobSessionId; isDone = true; ownershipRejected = false;
            targetSession = null;

            // Background -AsJob sessions are keyed with the creating request's
            // ASP.NET session id - which the current wait request will not share.
            // Find the session across any user-session by matching on just the
            // client-supplied persistent id.
            targetSession = ScriptSessionManager.GetMatchingSessionsForAnyUserSession(jobSessionId).FirstOrDefault();
            if (targetSession == null)
            {
                targetSession = ScriptSessionManager.GetSessionIfExists(jobSessionId);
            }
            if (targetSession == null) return false;

            // Ownership check uses the caller's own sessionId (the outer remoting
            // session). If omitted, fall back to the job session.
            ScriptSession ownerCheckSession = null;
            if (!string.IsNullOrEmpty(sessionId))
            {
                ownerCheckSession = ScriptSessionManager.GetMatchingSessionsForAnyUserSession(sessionId).FirstOrDefault()
                    ?? ScriptSessionManager.GetSessionIfExists(sessionId);
            }
            if (ownerCheckSession == null) ownerCheckSession = targetSession;

            if (!SessionOwnershipGuard.TryEnforce(context, ownerCheckSession, RemotingRequestContext.GetIp(context.Request)))
            {
                ownershipRejected = true;
                return true;
            }

            name = targetSession.ID ?? jobSessionId;
            var state = targetSession.State;
            var notBusy = state != System.Management.Automation.Runspaces.RunspaceAvailability.Busy;
            // A session whose runspace is Available *and* has never entered
            // ExecuteScriptPart is still pending - typically the AsJob's
            // Sitecore Job is queued but has not started running RunJob yet.
            // Treating that as Done would race the script-not-yet-started
            // window and trigger an immediate (false) completion. Wait until
            // we have actually seen the run start, then for it to leave Busy.
            isDone = targetSession.HasRunStarted && notBusy;
            if (!notBusy) status = "Busy";
            else if (!targetSession.HasRunStarted) status = "Pending";
            else status = "Idle";
            return true;
        }
    }
}
