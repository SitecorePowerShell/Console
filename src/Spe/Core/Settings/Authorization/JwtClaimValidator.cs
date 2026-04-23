using System;
using System.Collections.Generic;
using System.Linq;
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

        public static bool IsValidIssuer(string issuer, IList<string> allowed, bool suppressWarnings, bool detailedErrors)
        {
            var isValid = !string.IsNullOrEmpty(issuer) &&
                          allowed != null && allowed.Count > 0 &&
                          allowed.Any(i => string.Equals(i, issuer, StringComparison.OrdinalIgnoreCase));
            if (isValid) return true;

            if (!suppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=issuerNotAllowed issuer={LogSanitizer.SanitizeValue(issuer)}");
            if (detailedErrors) throw new SecurityException("The Token Issuer is not allowed.");
            return false;
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
