using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Script.Serialization;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    ///     Authentication provider that accepts external OAuth/OIDC bearer tokens (e.g. XM Cloud access tokens).
    ///     It validates the token's expiration, audience, and any required claims, then resolves the Sitecore
    ///     username from a configurable claim or a fixed service-account username.
    ///
    ///     JWT signature verification is intentionally skipped because in XM Cloud and other Sitecore Cloud
    ///     topologies the token has already been validated by the platform's edge/API-gateway layer before it
    ///     reaches the CM instance.  Operators that run SPE outside such a trusted proxy can front the endpoint
    ///     with their own gateway that validates the signature.
    /// </summary>
    public class OAuthBearerTokenAuthenticationProvider : ISpeAuthenticationProvider
    {
        // ── Configuration properties (populated from Sitecore config XML) ──────────────────────────────

        /// <summary>
        ///     Audiences that are allowed.  At least one of the values in the token's <c>aud</c> claim must
        ///     match an entry in this list (case-insensitive).  Configured via
        ///     <c>&lt;allowedAudiences hint="list"&gt;&lt;audience&gt;…&lt;/audience&gt;&lt;/allowedAudiences&gt;</c>.
        /// </summary>
        public List<string> AllowedAudiences { get; set; } = new List<string>();

        /// <summary>
        ///     Scope values that must ALL be present in the token's <c>scope</c> claim (space-delimited string
        ///     or array).  Configured via
        ///     <c>&lt;requiredScopes hint="list"&gt;&lt;scope&gt;…&lt;/scope&gt;&lt;/requiredScopes&gt;</c>.
        /// </summary>
        public List<string> RequiredScopes { get; set; } = new List<string>();

        /// <summary>
        ///     Name of the JWT claim whose value is used as the Sitecore username when
        ///     <see cref="ServiceAccountUsername"/> is not configured.  Defaults to <c>sub</c>.
        /// </summary>
        public string UsernameClaim { get; set; } = "sub";

        /// <summary>
        ///     When set, every successfully validated token causes this fixed Sitecore account to be used
        ///     regardless of the token's claims.  Useful for automation scenarios where all callers should
        ///     run as a shared service account (e.g. <c>sitecore\admin</c>).
        /// </summary>
        public string ServiceAccountUsername { get; set; }

        /// <summary>
        ///     When <c>true</c>, authentication errors include additional detail in log messages.
        /// </summary>
        public bool DetailedAuthenticationErrors { get; set; }

        // ── ISpeAuthenticationProvider ───────────────────────────────────────────────────────────────

        public bool Validate(string token, string authority, out string username)
        {
            username = null;

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var parts = token.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                LogDetail("OAuth bearer token is not a valid three-part JWT.");
                return false;
            }

            // ── Decode header (not strictly needed here but validates structure) ──────────────────
            string payloadJson;
            try
            {
                payloadJson = Encoding.UTF8.GetString(Base64UrlDecode(parts[1]));
            }
            catch (Exception ex)
            {
                LogDetail($"OAuth bearer token payload could not be decoded: {ex.Message}");
                return false;
            }

            var serializer = new JavaScriptSerializer();
            Dictionary<string, object> payload;
            try
            {
                payload = serializer.Deserialize<Dictionary<string, object>>(payloadJson);
            }
            catch (Exception ex)
            {
                LogDetail($"OAuth bearer token payload could not be parsed: {ex.Message}");
                return false;
            }

            if (!ValidateExpiration(payload)) return false;
            if (!ValidateAudience(payload)) return false;
            if (!ValidateScopes(payload)) return false;

            username = ResolveUsername(payload);
            if (string.IsNullOrEmpty(username))
            {
                LogDetail($"OAuth bearer token does not contain the required username claim '{UsernameClaim}'.");
                return false;
            }

            PowerShellLog.Debug($"OAuthBearerTokenAuthenticationProvider: token accepted, resolved username '{username}'.");
            return true;
        }

        // ── Validation helpers ───────────────────────────────────────────────────────────────────────

        private bool ValidateExpiration(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("exp", out var expObj))
            {
                LogDetail("OAuth bearer token is missing the 'exp' claim.");
                return false;
            }

            long exp;
            try
            {
                exp = Convert.ToInt64(expObj);
            }
            catch
            {
                LogDetail("OAuth bearer token 'exp' claim could not be converted to a long.");
                return false;
            }

            var expireUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(exp);
            if (DateTime.UtcNow >= expireUtc)
            {
                LogDetail("OAuth bearer token has expired.");
                return false;
            }

            return true;
        }

        private bool ValidateAudience(Dictionary<string, object> payload)
        {
            if (AllowedAudiences == null || AllowedAudiences.Count == 0)
            {
                // No audience restriction configured – allow any audience.
                return true;
            }

            if (!payload.TryGetValue("aud", out var audObj))
            {
                LogDetail("OAuth bearer token is missing the 'aud' claim but audiences are required.");
                return false;
            }

            // aud can be a single string or a JSON array.
            var tokenAudiences = new List<string>();
            if (audObj is string s)
            {
                tokenAudiences.Add(s);
            }
            else if (audObj is System.Collections.ArrayList arr)
            {
                foreach (var item in arr) tokenAudiences.Add(item?.ToString());
            }

            foreach (var allowed in AllowedAudiences)
            {
                foreach (var tokenAud in tokenAudiences)
                {
                    if (string.Equals(allowed, tokenAud, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }

            LogDetail($"OAuth bearer token audience does not match any allowed audience.");
            return false;
        }

        private bool ValidateScopes(Dictionary<string, object> payload)
        {
            if (RequiredScopes == null || RequiredScopes.Count == 0)
            {
                return true;
            }

            payload.TryGetValue("scope", out var scopeObj);
            var tokenScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (scopeObj is string scopeStr)
            {
                foreach (var s in scopeStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    tokenScopes.Add(s);
            }
            else if (scopeObj is System.Collections.ArrayList arr)
            {
                foreach (var item in arr) tokenScopes.Add(item?.ToString());
            }

            foreach (var required in RequiredScopes)
            {
                if (!tokenScopes.Contains(required))
                {
                    LogDetail($"OAuth bearer token is missing required scope '{required}'.");
                    return false;
                }
            }

            return true;
        }

        private string ResolveUsername(Dictionary<string, object> payload)
        {
            if (!string.IsNullOrEmpty(ServiceAccountUsername))
            {
                return ServiceAccountUsername;
            }

            var claimName = string.IsNullOrEmpty(UsernameClaim) ? "sub" : UsernameClaim;
            payload.TryGetValue(claimName, out var claimValue);
            return claimValue?.ToString();
        }

        // ── Utility ─────────────────────────────────────────────────────────────────────────────────

        private void LogDetail(string message)
        {
            if (DetailedAuthenticationErrors)
            {
                PowerShellLog.Warn($"OAuthBearerTokenAuthenticationProvider: {message}");
            }
            else
            {
                PowerShellLog.Debug($"OAuthBearerTokenAuthenticationProvider: {message}");
            }
        }

        private static byte[] Base64UrlDecode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input));

            var output = input
                .Replace('-', '+')
                .Replace('_', '/');

            switch (output.Length % 4)
            {
                case 2: output += "=="; break;
                case 3: output += "="; break;
            }

            return Convert.FromBase64String(output);
        }
    }
}
