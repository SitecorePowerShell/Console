using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class RemotingRequestContext
    {
        public static string GetIp(HttpRequest request)
        {
            // Cached per-request: GetIp is called 15+ times along the pipeline
            // and every uncached call touches ServerVariables, which is a
            // lazy-populated collection backed by IIS worker-process state.
            var ctx = HttpContext.Current;
            if (ctx != null && ctx.Items[RemotingHttpContextKeys.ClientIp] is string cached) return cached;

            var ip = ResolveIp(request);

            if (ctx != null && !string.IsNullOrEmpty(ip))
            {
                ctx.Items[RemotingHttpContextKeys.ClientIp] = ip;
            }
            return ip;
        }

        public static string ResolveIp(HttpRequest request)
        {
            if (WebServiceSettings.UseForwardedHeaders)
            {
                var forwarded = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (ForwardedHeaderHelper.TryGetClientIp(forwarded, out var clientIp))
                    return clientIp;
            }
            return request.ServerVariables["REMOTE_ADDR"];
        }

        public static string ComputeScriptHash(string script)
        {
            if (string.IsNullOrEmpty(script)) return "empty";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(script));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 16);
            }
        }

        // Returns the correlation ID for the current HTTP request, generating one if needed.
        public static string GetRequestId() => GetRequestId(HttpContext.Current);

        // Returns the correlation ID for the supplied HTTP request, generating
        // one if needed. Prefer this overload from async paths where
        // HttpContext.Current may be null after an await.
        public static string GetRequestId(HttpContext ctx)
        {
            if (ctx == null) return "no-ctx";
            var rid = ctx.Items[RemotingHttpContextKeys.RequestId] as string;
            if (rid != null) return rid;
            rid = Guid.NewGuid().ToString("N").Substring(0, 8);
            ctx.Items[RemotingHttpContextKeys.RequestId] = rid;
            return rid;
        }

        // Resolves the active remoting policy from the current HTTP context.
        // Returns RemotingPolicy.Unrestricted when no Remoting Client is
        // present (config-based shared-secret sessions).
        public static RemotingPolicy GetActivePolicy()
        {
            var client = HttpContext.Current?.Items[RemotingHttpContextKeys.RemotingClient] as RemotingClient;
            if (client == null) return RemotingPolicy.Unrestricted;
            return RemotingPolicyManager.ResolvePolicy(client.Policy);
        }

        // Build the ownership identity string for the current request. Format
        // "remotingClient:<name>" when a Remoting Client authenticated the
        // request, otherwise "user:<username>" for config-based shared-secret
        // or Basic auth. Reads off the supplied context (not HttpContext.Current)
        // because the long-poll wait path resumes on a thread-pool continuation
        // where the ambient HttpContext.Current is null.
        public static string GetRequestOwnershipIdentity(HttpContext context)
        {
            var client = context?.Items[RemotingHttpContextKeys.RemotingClient] as RemotingClient;
            if (client != null)
            {
                return "remotingClient:" + client.Name;
            }
            var user = Sitecore.Context.User?.Name;
            return string.IsNullOrEmpty(user) ? null : "user:" + user;
        }
    }
}
