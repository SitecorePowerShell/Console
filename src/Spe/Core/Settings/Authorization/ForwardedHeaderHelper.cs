using System;
using System.Net;

namespace Spe.Core.Settings.Authorization
{
    // Pure helpers backing the Spe.Remoting.UseForwardedHeaders gate. Kept dependency-free
    // so they are unit-testable without an HttpContext.
    public static class ForwardedHeaderHelper
    {
        public static bool ShouldAcceptForwardedProto(
            bool requireSecureConnection,
            bool isSecureConnection,
            string forwardedProtoHeader,
            bool useForwardedHeaders)
        {
            if (!requireSecureConnection) return true;
            if (isSecureConnection) return true;
            if (!useForwardedHeaders) return false;
            return string.Equals(forwardedProtoHeader, Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase);
        }

        // Returns the leftmost token of an X-Forwarded-For chain only when it
        // parses as a valid IP. Validation closes the log-injection sub-issue:
        // CR/LF and other non-IP content cannot survive IPAddress.TryParse.
        public static bool TryGetClientIp(string headerValue, out string ip)
        {
            ip = null;
            if (string.IsNullOrWhiteSpace(headerValue)) return false;
            var firstToken = headerValue.Split(',')[0].Trim();
            // X-Forwarded-For carries bare IPs per the de-facto spec - the
            // bracketed [ipv6]:port form belongs to the RFC 7239 Forwarded:
            // header and IPAddress.TryParse on .NET Framework 4.8 silently
            // accepts it, so reject brackets explicitly to avoid logging
            // non-standard "ip:port" strings as if they were IPs.
            if (firstToken.IndexOf('[') >= 0 || firstToken.IndexOf(']') >= 0) return false;
            if (IPAddress.TryParse(firstToken, out _))
            {
                ip = firstToken;
                return true;
            }
            return false;
        }
    }
}
