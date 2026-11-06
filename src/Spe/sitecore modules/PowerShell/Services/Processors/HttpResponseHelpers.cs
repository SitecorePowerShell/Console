using System.Web;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class HttpResponseHelpers
    {
        public static void AddThrottleHeaders(HttpResponse response, RemotingClientProvider.ThrottleResult throttle)
        {
            if (!throttle.HasLimit) return;

            response.Headers["X-RateLimit-Limit"] = throttle.Limit.ToString();
            response.Headers["X-RateLimit-Remaining"] = throttle.Remaining.ToString();
            response.Headers["X-RateLimit-Reset"] = throttle.ResetUnixTimestamp.ToString();

            if (!throttle.Allowed)
            {
                response.Headers["Retry-After"] = throttle.RetryAfterSeconds.ToString();
            }
        }

        public static void AddCorsHeaders(HttpContext context, string serviceName, string origin)
        {
            if (string.IsNullOrEmpty(origin))
            {
                return;
            }

            var cors = WebServiceSettings.GetCorsSettings(serviceName);
            if (cors == null)
            {
                return;
            }

            if (!IsOriginAllowed(cors, origin))
            {
                return;
            }

            var response = context.Response;
            response.Headers["Access-Control-Allow-Origin"] = cors.AllowAnyOrigin ? "*" : origin;

            if (cors.AllowCredentials)
            {
                response.Headers["Access-Control-Allow-Credentials"] = "true";
            }
        }

        private static bool IsOriginAllowed(WebServiceSettings.CorsSettings cors, string origin)
        {
            if (cors.AllowAnyOrigin)
            {
                return true;
            }

            return !string.IsNullOrEmpty(origin) && cors.AllowedOrigins != null && cors.AllowedOrigins.Contains(origin);
        }
    }
}
