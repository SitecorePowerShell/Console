using System;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using Spe.Core.Diagnostics;
using Spe.Core.Host;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class BuiltinActionProcessor
    {
        public static void ProcessCleanup(HttpContext context, string sessionId, string user)
        {
            var ip = RemotingRequestContext.GetIp(context.Request);

            if (string.IsNullOrEmpty(sessionId))
            {
                ErrorResponseWriter.SetErrorResponse(context, 400, "Session ID is required for cleanup.");
                return;
            }

            var disposed = 0;
            foreach (var session in ScriptSessionManager.GetMatchingSessionsForAnyUserSession(sessionId).ToList())
            {
                if (session.ApplianceType == "RemoteAutomation")
                {
                    ScriptSessionManager.RemoveSession(session);
                    disposed++;
                }
            }

            if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=sessionCleanup user={0} ip={1} sessionId={2} disposed={3} rid={4}",
                    user, ip, sessionId, disposed, RemotingRequestContext.GetRequestId());
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.Write($"Disposed {disposed} session(s).");
        }

        public static void ProcessTest(HttpContext context, string user)
        {
            var ip = RemotingRequestContext.GetIp(context.Request);

            if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=connectionTest user={0} ip={1} rid={2}", user, ip, RemotingRequestContext.GetRequestId());
            }

            var result = new
            {
                SPEVersion = CurrentVersion.SpeVersion.ToString(),
                SitecoreVersion = SitecoreVersion.Current.ToString(),
                CurrentTime = DateTime.UtcNow.ToString("o")
            };

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.Write(new JavaScriptSerializer().Serialize(result));
        }
    }
}
