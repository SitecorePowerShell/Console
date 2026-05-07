using System;
using System.Web;
using Spe.Core.Diagnostics;
using Spe.Core.Host;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    // Claim-on-first-access ownership for ScriptSession. The first authenticated
    // identity that touches a session id wins; subsequent calls under a different
    // identity get 403 + an audit line. Used by ScriptProcessor (script execution)
    // and WaitProcessor (long-poll). Ownership lives in HttpRuntime.Cache so it
    // survives across requests, independent of the ASP.NET session-cookie lifecycle.
    internal static class SessionOwnershipGuard
    {
        // Mirrored on ScriptSession.CreatedByIdentity for in-process observability;
        // the cache entry is the source of truth.
        private const string SessionOwnerCacheKey = "SPE.SessionOwner|";
        private static readonly TimeSpan SessionOwnerTtl = TimeSpan.FromMinutes(30);

        public static bool TryEnforce(HttpContext context, ScriptSession session, string ip)
        {
            if (session == null) return true;

            var identity = RemotingRequestContext.GetRequestOwnershipIdentity(context);
            if (string.IsNullOrEmpty(identity)) return true;

            var clientSessionId = session.ID;
            if (string.IsNullOrEmpty(clientSessionId)) return true;

            var cacheKey = SessionOwnerCacheKey + clientSessionId;
            var recordedOwner = HttpRuntime.Cache.Get(cacheKey) as string;

            if (string.IsNullOrEmpty(recordedOwner))
            {
                HttpRuntime.Cache.Insert(cacheKey, identity, null,
                    DateTime.UtcNow.Add(SessionOwnerTtl),
                    System.Web.Caching.Cache.NoSlidingExpiration);
                session.CreatedByIdentity = identity;
                return true;
            }

            if (string.Equals(recordedOwner, identity, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(session.CreatedByIdentity))
                {
                    session.CreatedByIdentity = recordedOwner;
                }
                return true;
            }

            PowerShellLog.Audit("[Remoting] action=sessionOwnershipMismatch session={0} owner={1} caller={2} ip={3} rid={4}",
                clientSessionId, recordedOwner, identity, ip, RemotingRequestContext.GetRequestId(context));
            context.Response.Headers["X-Spe-Restriction"] = "session-not-owned";
            ErrorResponseWriter.SetErrorResponse(context, 403, "Session is owned by a different identity.");
            return false;
        }
    }
}
