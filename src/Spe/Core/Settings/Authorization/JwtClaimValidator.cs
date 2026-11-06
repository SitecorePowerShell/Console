using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Exceptions;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    internal static class JwtClaimValidator
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        public static bool IsValidExpiration(long exp, int clockSkewSeconds, bool suppressWarnings, bool detailedErrors)
        {
            var nowUtc = DateTime.UtcNow;
            var expireUtc = Epoch.AddSeconds(exp);
            var isValid = nowUtc < expireUtc.AddSeconds(clockSkewSeconds);
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=tokenExpired expiry={expireUtc:O}");
            if (detailedErrors) throw new SecurityException("The Token Expiration has passed.");
            return false;
        }

        public static bool IsValidNotBefore(long nbf, int clockSkewSeconds, bool suppressWarnings, bool detailedErrors)
        {
            var nbfUtc = Epoch.AddSeconds(nbf);
            var isValid = DateTime.UtcNow >= nbfUtc.AddSeconds(-clockSkewSeconds);
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=notYetValid nbf={nbfUtc:O}");
            if (detailedErrors) throw new SecurityException("The Token is not yet valid (nbf claim).");
            return false;
        }

        public static bool IsValidIssuedAt(long iat, int clockSkewSeconds, bool suppressWarnings, bool detailedErrors)
        {
            var iatUtc = Epoch.AddSeconds(iat);
            var isValid = DateTime.UtcNow >= iatUtc.AddSeconds(-clockSkewSeconds);
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=issuedInFuture iat={iatUtc:O}");
            if (detailedErrors) throw new SecurityException("The Token issued-at time is in the future (iat claim).");
            return false;
        }

        public static bool IsValidTokenLifetime(long exp, long iat, long maxLifetimeSeconds, bool suppressWarnings, bool detailedErrors)
        {
            var lifetime = exp - iat;
            var isValid = lifetime <= maxLifetimeSeconds;
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=lifetimeExceeded lifetime={lifetime}s maximum={maxLifetimeSeconds}s");
            if (detailedErrors) throw new SecurityException($"The Token lifetime ({lifetime}s) exceeds the maximum allowed ({maxLifetimeSeconds}s).");
            return false;
        }

        public static bool IsValidAudience(string audience, IList<string> allowed, bool suppressWarnings, bool detailedErrors)
        {
            PowerShellLog.Debug($"[JWT] action=audienceCheck audience={LogSanitizer.SanitizeValue(audience)}");
            var isValid = !string.IsNullOrEmpty(audience) &&
                          allowed != null && allowed.Count > 0 &&
                          allowed.Any(a => string.Equals(a, audience, StringComparison.OrdinalIgnoreCase));
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=audienceNotAllowed audience={LogSanitizer.SanitizeValue(audience)}");
            if (detailedErrors) throw new SecurityException("The Token Audience is not allowed.");
            return false;
        }

        // Pure-function token-type check. Caller wraps with warn-log + throw decision.
        // Default mode accepts no typ, "JWT", or "at+jwt". Strict mode requires "at+jwt"
        // exactly (RFC 9068).
        public static bool IsValidTokenType(string typ, bool requireAccessTokenType)
        {
            if (requireAccessTokenType)
            {
                return string.Equals(typ, "at+jwt", StringComparison.OrdinalIgnoreCase);
            }
            if (string.IsNullOrEmpty(typ)) return true;
            return string.Equals(typ, "JWT", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(typ, "at+jwt", StringComparison.OrdinalIgnoreCase);
        }

        // Pure-function azp check (OIDC Core 2 Section 3.1.3.7, RFC 7519 Section 4.1.3).
        // Skipped when not required, when no aud, or when aud is single-valued.
        // For multi-audience tokens, azp must be present and match the resolved
        // client_id used for OAuth Client item lookup.
        public static bool IsValidAzp(IList<string> audiences, string azp, string resolvedClientId, bool requireAzpWhenMultiAudience)
        {
            if (!requireAzpWhenMultiAudience) return true;
            if (audiences == null || audiences.Count <= 1) return true;
            if (string.IsNullOrEmpty(azp)) return false;
            return string.Equals(azp, resolvedClientId, StringComparison.Ordinal);
        }

        public static bool IsValidIssuer(string issuer, IList<string> allowed, bool suppressWarnings, bool detailedErrors)
        {
            var canonicalIssuer = CanonicalizeIssuer(issuer);
            var isValid = !string.IsNullOrEmpty(canonicalIssuer) &&
                          allowed != null && allowed.Count > 0 &&
                          allowed.Any(a => string.Equals(CanonicalizeIssuer(a), canonicalIssuer, StringComparison.Ordinal));
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=issuerNotAllowed issuer={LogSanitizer.SanitizeValue(issuer)}");
            if (detailedErrors) throw new SecurityException("The Token Issuer is not allowed.");
            return false;
        }

        // Normalize an issuer for comparison. URI issuers: lowercase scheme +
        // host, strip default port, strip trailing slash on path, preserve
        // path case (RFC 3986 path is case-sensitive). Opaque issuers
        // (non-URI strings like "SPE Remoting"): lowercase the whole value
        // so callers can compare Ordinal and still get case-insensitive
        // matching for the legacy SharedSecret values. Eliminates the
        // silent-rejection class where operator-configured allowedIssuers
        // and IdP-emitted iss differ by trailing slash or host case.
        public static string CanonicalizeIssuer(string issuer)
        {
            if (issuer == null) return null;
            if (issuer.Length == 0) return string.Empty;

            if (!Uri.TryCreate(issuer, UriKind.Absolute, out var uri))
            {
                return issuer.ToLowerInvariant();
            }

            var sb = new StringBuilder();
            sb.Append(uri.Scheme.ToLowerInvariant());
            sb.Append("://");
            sb.Append(uri.Host.ToLowerInvariant());

            if (!uri.IsDefaultPort)
            {
                sb.Append(':');
                sb.Append(uri.Port);
            }

            var path = uri.AbsolutePath;
            if (path == "/")
            {
                // Root path collapses to empty so https://idp/ == https://idp.
            }
            else if (path.Length > 1 && path[path.Length - 1] == '/')
            {
                sb.Append(path.Substring(0, path.Length - 1));
            }
            else
            {
                sb.Append(path);
            }

            if (!string.IsNullOrEmpty(uri.Query)) sb.Append(uri.Query);
            if (!string.IsNullOrEmpty(uri.Fragment)) sb.Append(uri.Fragment);

            return sb.ToString();
        }

        // Build the value portion of a WWW-Authenticate response header
        // (RFC 6750 Section 3). Internal failure reasons are mapped to the
        // RFC's two error codes; descriptions are added only where they
        // don't risk leaking enumeration signal. Disabled and unknown
        // reasons collapse to bare invalid_token. Caller writes the
        // resulting string to the WWW-Authenticate header.
        public static string BuildWwwAuthenticate(string failureReason)
        {
            switch (failureReason)
            {
                case "expired":
                    return "Bearer error=\"invalid_token\", error_description=\"The access token expired\"";
                case "replay":
                    return "Bearer error=\"invalid_token\", error_description=\"The access token has already been used\"";
                case "missing_scope":
                    return "Bearer error=\"insufficient_scope\"";
                default:
                    return "Bearer error=\"invalid_token\"";
            }
        }

        public static byte[] Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input));

            var output = input;
            output = output.Replace('-', '+');
            output = output.Replace('_', '/');
            switch (output.Length % 4)
            {
                case 0:
                    break;
                case 2:
                    output += "==";
                    break;
                case 3:
                    output += "=";
                    break;
                default:
                    throw new FormatException("Illegal base64url string.");
            }
            return Convert.FromBase64String(output);
        }
    }
}
