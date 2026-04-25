using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using Sitecore.Exceptions;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    public class OAuthBearerTokenAuthenticationProvider : ISpeAuthenticationProviderEx
    {
        private static readonly HashSet<string> RsaAlgorithms =
            new HashSet<string>(StringComparer.Ordinal) { "RS256", "RS384", "RS512" };

        private static readonly HashSet<string> EcdsaAlgorithms =
            new HashSet<string>(StringComparer.Ordinal) { "ES256", "ES384", "ES512" };

        public List<string> AllowedAudiences { get; set; }
        public List<string> AllowedIssuers { get; set; }
        public List<string> RequiredScopes { get; set; }
        public List<string> AllowedAlgorithms { get; set; }
        public string JwksUri { get; set; }
        public int JwksCacheSeconds { get; set; }
        public int MaxTokenLifetimeSeconds { get; set; }
        public bool DetailedAuthenticationErrors { get; set; }
        public bool SuppressWarnings { get; set; }
        public int ClockSkewSeconds { get; set; }
        public bool JtiReplayCacheEnabled { get; set; }
        public int JtiReplayCacheMaxEntries { get; set; }
        public bool RequireAccessTokenType { get; set; }
        public bool RequireAzpWhenMultiAudience { get; set; }
        public bool JwksAllowLoopbackHttp { get; set; }

        private JtiReplayCache _replayCache;

        public OAuthBearerTokenAuthenticationProvider()
        {
            AllowedAudiences = new List<string>();
            AllowedIssuers = new List<string>();
            RequiredScopes = new List<string>();
            AllowedAlgorithms = new List<string> { "RS256", "RS384", "RS512", "ES256" };
            JwksCacheSeconds = 600;
            MaxTokenLifetimeSeconds = 3600;
            ClockSkewSeconds = 30;
            JtiReplayCacheMaxEntries = 10000;
        }

        // Lazy: Sitecore Factory.CreateObject sets properties post-construction,
        // so we cannot materialise the cache in the constructor and read the
        // operator-supplied MaxEntries value.
        private JtiReplayCache GetReplayCache()
        {
            if (!JtiReplayCacheEnabled) return null;
            return LazyInitializer.EnsureInitialized(
                ref _replayCache,
                () => new JtiReplayCache(JtiReplayCacheMaxEntries));
        }

        // Test seam.
        internal JtiReplayCache ReplayCache => _replayCache;

        public bool Validate(string token, string authority, out string username)
        {
            return ValidateToken(token, out username, out _);
        }

        public bool Validate(string token, string authority, out string username, out TokenValidationResult result, string sharedSecretOverride = null)
        {
            // Bearer provider ignores authority and sharedSecretOverride: audience/issuer come from config,
            // signature comes from JWKS. Kept in the signature so the dispatcher can call either provider.
            return ValidateToken(token, out username, out result);
        }

        private bool ValidateToken(string token, out string username, out TokenValidationResult result)
        {
            username = null;
            result = null;
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=malformedToken");
                    return false;
                }

                var serializer = new JavaScriptSerializer();

                var headerJsonBase64 = parts[0];
                var payloadJsonBase64 = parts[1];
                var signatureBase64 = parts[2];

                var headerJson = Encoding.UTF8.GetString(JwtClaimValidator.Decode(headerJsonBase64));
                var header = serializer.Deserialize<Dictionary<string, object>>(headerJson);

                // typ is optional per RFC 7519. Default accepts no typ, "JWT", or
                // "at+jwt" (RFC 9068, emitted by Duende / Sitecore Identity Server).
                // RequireAccessTokenType narrows to "at+jwt" only - closes the OIDC
                // ID-token-as-access-token confusion class on IdPs that stamp at+jwt
                // for access tokens (Okta, Duende v5+, Ping).
                var typ = header.TryGetValue("typ", out var typObj) ? typObj?.ToString() : null;
                if (!JwtClaimValidator.IsValidTokenType(typ, RequireAccessTokenType))
                {
                    var reason = RequireAccessTokenType ? "accessTokenTypeRequired" : "invalidTokenType";
                    if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason={reason} type={LogSanitizer.SanitizeValue(typ)}");
                    if (DetailedAuthenticationErrors) throw new SecurityException(RequireAccessTokenType ? "The Token Type must be at+jwt." : "The Token Type is incorrect.");
                    return false;
                }

                var alg = header.TryGetValue("alg", out var algObj) ? algObj?.ToString() : null;
                if (string.IsNullOrEmpty(alg) || !AllowedAlgorithms.Contains(alg, StringComparer.Ordinal))
                {
                    if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=algorithmNotAllowed algorithm={LogSanitizer.SanitizeValue(alg)}");
                    if (DetailedAuthenticationErrors) throw new SecurityException("The Token algorithm is not allowed.");
                    return false;
                }

                var kid = header.TryGetValue("kid", out var kidObj) ? kidObj?.ToString() : null;

                var payloadJson = Encoding.UTF8.GetString(JwtClaimValidator.Decode(payloadJsonBase64));
                var payload = serializer.Deserialize<Dictionary<string, object>>(payloadJson);

                if (!TryGetLong(payload, "exp", out var exp) ||
                    !JwtClaimValidator.IsValidExpiration(exp, ClockSkewSeconds, SuppressWarnings, DetailedAuthenticationErrors))
                {
                    return false;
                }

                if (TryGetLong(payload, "nbf", out var nbf) && nbf > 0 &&
                    !JwtClaimValidator.IsValidNotBefore(nbf, ClockSkewSeconds, SuppressWarnings, DetailedAuthenticationErrors))
                {
                    return false;
                }

                if (!TryGetLong(payload, "iat", out var iat))
                {
                    if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=missingIat");
                    if (DetailedAuthenticationErrors) throw new SecurityException("The Token iat claim is required.");
                    return false;
                }

                if (!JwtClaimValidator.IsValidIssuedAt(iat, ClockSkewSeconds, SuppressWarnings, DetailedAuthenticationErrors))
                {
                    return false;
                }

                if (MaxTokenLifetimeSeconds > 0 &&
                    !JwtClaimValidator.IsValidTokenLifetime(exp, iat, MaxTokenLifetimeSeconds, SuppressWarnings, DetailedAuthenticationErrors))
                {
                    return false;
                }

                var issuer = payload.TryGetValue("iss", out var issObj) ? issObj?.ToString() : null;
                if (!JwtClaimValidator.IsValidIssuer(issuer, AllowedIssuers, SuppressWarnings, DetailedAuthenticationErrors))
                {
                    return false;
                }

                if (!ValidateAudience(payload))
                {
                    return false;
                }

                if (!ValidateScopes(payload))
                {
                    return false;
                }

                PowerShellLog.Debug($"[OAuthBearer] action=tokenParsed issuer={LogSanitizer.SanitizeValue(issuer)} algorithm={LogSanitizer.SanitizeValue(alg)} kid={LogSanitizer.SanitizeValue(kid)}");

                if (!VerifySignature(alg, kid, headerJsonBase64, payloadJsonBase64, signatureBase64))
                {
                    return false;
                }

                var replayCache = GetReplayCache();
                if (replayCache != null)
                {
                    var jti = payload.TryGetValue("jti", out var jtiObj) ? jtiObj?.ToString() : null;
                    if (string.IsNullOrEmpty(jti))
                    {
                        if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=missingJti");
                        if (DetailedAuthenticationErrors) throw new SecurityException("The Token jti claim is required when replay protection is enabled.");
                        return false;
                    }

                    if (!replayCache.TryClaim(issuer, jti, exp))
                    {
                        result = new TokenValidationResult { Issuer = issuer, FailureReason = "replay" };
                        if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=tokenReplay jti={LogSanitizer.SanitizeValue(jti)}");
                        if (DetailedAuthenticationErrors) throw new SecurityException("The Token has already been used (jti replay).");
                        return false;
                    }
                }

                // Identity comes from the OAuth Client item's ImpersonateUser
                // field, looked up by the handler via (iss, client_id) after
                // we return. We don't compute a fallback username from token
                // claims - if no item match supplies identity, the handler
                // rejects with 401.
                username = string.Empty;

                var clientId = ResolveClientId(payload);
                result = new TokenValidationResult
                {
                    Issuer = issuer,
                    ClientId = clientId
                };
                return true;
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception ex)
            {
                PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=unexpected error={LogSanitizer.SanitizeValue(ex.GetType().Name)}");
                return false;
            }
        }

        private bool ValidateAudience(Dictionary<string, object> payload)
        {
            if (!payload.TryGetValue("aud", out var audObj) || audObj == null)
            {
                if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=missingAudience");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token Audience is missing.");
                return false;
            }

            var candidates = FlattenStringOrArray(audObj).ToList();
            if (candidates.Count == 0)
            {
                if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=missingAudience");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token Audience is missing.");
                return false;
            }

            var matched = false;
            foreach (var candidate in candidates)
            {
                if (JwtClaimValidator.IsValidAudience(candidate, AllowedAudiences, suppressWarnings: true, detailedErrors: false))
                {
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=audienceNotAllowed audience={LogSanitizer.SanitizeValue(string.Join(",", candidates))}");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token Audience is not allowed.");
                return false;
            }

            // Multi-audience azp check. Skipped when single-aud or flag off.
            if (RequireAzpWhenMultiAudience && candidates.Count > 1)
            {
                var azp = payload.TryGetValue("azp", out var azpObj) ? azpObj?.ToString() : null;
                var resolvedClientId = ResolveClientId(payload);
                if (!JwtClaimValidator.IsValidAzp(candidates, azp, resolvedClientId, true))
                {
                    if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=azpMismatch azp={LogSanitizer.SanitizeValue(azp)} clientId={LogSanitizer.SanitizeValue(resolvedClientId)}");
                    if (DetailedAuthenticationErrors) throw new SecurityException("The Token azp claim is missing or does not match the resolved client_id.");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateScopes(Dictionary<string, object> payload)
        {
            if (RequiredScopes == null || RequiredScopes.Count == 0) return true;

            IEnumerable<string> tokenScopes = null;

            // "scope" can be a space-delimited string (RFC 8693) OR an array (Duende / IS4 JSON),
            // "scp" (Azure AD) is typically an array. Check scope first, array-aware.
            if (payload.TryGetValue("scope", out var scopeObj) && scopeObj != null)
            {
                if (scopeObj is string s && !string.IsNullOrWhiteSpace(s))
                {
                    tokenScopes = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                }
                else
                {
                    tokenScopes = FlattenStringOrArray(scopeObj);
                }
            }
            else if (payload.TryGetValue("scp", out var scpObj) && scpObj != null)
            {
                tokenScopes = FlattenStringOrArray(scpObj);
            }

            var tokenScopeSet = new HashSet<string>(
                tokenScopes ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            var missing = RequiredScopes
                .Where(required => !tokenScopeSet.Contains(required))
                .ToList();

            if (missing.Count == 0) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=missingScope missing={LogSanitizer.SanitizeValue(string.Join(",", missing))}");
            if (DetailedAuthenticationErrors) throw new SecurityException("One or more required scopes are missing.");
            return false;
        }

        private bool VerifySignature(string alg, string kid, string headerBase64, string payloadBase64, string signatureBase64)
        {
            if (string.IsNullOrWhiteSpace(JwksUri))
            {
                if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=jwksUriNotConfigured");
                if (DetailedAuthenticationErrors) throw new SecurityException("The JwksUri is not configured.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(kid))
            {
                if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=missingKid");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token kid claim is missing.");
                return false;
            }

            var key = JwksKeyResolver.GetKey(JwksUri, kid, JwksCacheSeconds, JwksAllowLoopbackHttp);
            if (key == null)
            {
                if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=keyNotResolved kid={LogSanitizer.SanitizeValue(kid)}");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token signing key could not be resolved.");
                return false;
            }

            if (!string.IsNullOrEmpty(key.Alg) && !string.Equals(key.Alg, alg, StringComparison.Ordinal))
            {
                if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=keyAlgorithmMismatch kid={LogSanitizer.SanitizeValue(kid)} headerAlg={LogSanitizer.SanitizeValue(alg)} keyAlg={LogSanitizer.SanitizeValue(key.Alg)}");
                if (DetailedAuthenticationErrors) throw new SecurityException("The signing key algorithm does not match the token header.");
                return false;
            }

            var signedBytes = JwksKeyResolver.GetSignedBytes($"{headerBase64}.{payloadBase64}");
            byte[] signatureBytes;
            try
            {
                signatureBytes = JwksKeyResolver.DecodeSignature(signatureBase64);
            }
            catch
            {
                if (!SuppressWarnings) PowerShellLog.Warn("[OAuthBearer] action=validationFailed reason=malformedSignature");
                if (DetailedAuthenticationErrors) throw new SecurityException("The Token signature is malformed.");
                return false;
            }

            bool verified;
            if (RsaAlgorithms.Contains(alg))
            {
                verified = JwksKeyResolver.VerifyRsa(key, alg, signedBytes, signatureBytes);
            }
            else if (EcdsaAlgorithms.Contains(alg))
            {
                verified = JwksKeyResolver.VerifyEcdsa(key, alg, signedBytes, signatureBytes);
            }
            else
            {
                verified = false;
            }

            if (verified) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[OAuthBearer] action=validationFailed reason=signatureInvalid algorithm={LogSanitizer.SanitizeValue(alg)} kid={LogSanitizer.SanitizeValue(kid)}");
            if (DetailedAuthenticationErrors) throw new SecurityException("The Token signature is not valid.");
            return false;
        }

        // Decode the JWT payload and read the iss claim WITHOUT verifying the
        // signature. Used by the handler to route incoming tokens to the
        // provider configured for that issuer before we pay the cost of
        // signature verification. An attacker can spoof iss, but our caller
        // only uses it to pick which provider's JWKS the token must then
        // verify against - a spoofed iss just gets routed to the matching
        // provider which then rejects the bad signature. Not a bypass.
        public static bool TryPeekIssuer(string token, out string issuer)
        {
            issuer = null;
            if (string.IsNullOrEmpty(token)) return false;

            var parts = token.Split('.');
            if (parts.Length < 2) return false;

            try
            {
                var payloadJson = Encoding.UTF8.GetString(JwtClaimValidator.Decode(parts[1]));
                var serializer = new JavaScriptSerializer();
                var payload = serializer.Deserialize<Dictionary<string, object>>(payloadJson);
                if (payload == null) return false;

                if (payload.TryGetValue("iss", out var issObj))
                {
                    var value = issObj?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        issuer = value;
                        return true;
                    }
                }
            }
            catch
            {
                // Malformed payload - caller treats null iss as "reject".
            }
            return false;
        }

        // Pick the OAuth provider that will validate this token. Matches on
        // both algorithm (so an RS-signed token isn't routed to an ES-only
        // provider) and issuer (so two providers declared for different IdPs
        // route deterministically, not last-declared-wins on alg alone).
        // Returns null if no provider covers both dimensions - caller 401s.
        public static OAuthBearerTokenAuthenticationProvider SelectProvider(
            IReadOnlyList<ISpeAuthenticationProvider> providers, string alg, string issuer)
        {
            if (providers == null || string.IsNullOrEmpty(alg) || string.IsNullOrEmpty(issuer))
            {
                return null;
            }

            foreach (var candidate in providers)
            {
                if (!(candidate is OAuthBearerTokenAuthenticationProvider oauth)) continue;
                if (oauth.AllowedAlgorithms == null || !oauth.AllowedAlgorithms.Contains(alg)) continue;
                if (oauth.AllowedIssuers == null) continue;
                foreach (var allowed in oauth.AllowedIssuers)
                {
                    if (string.Equals(allowed, issuer, StringComparison.OrdinalIgnoreCase))
                    {
                        return oauth;
                    }
                }
            }
            return null;
        }

        // Client id claim fallback chain. Different IdPs stamp the caller's
        // client id into different claims:
        //   client_id : OIDC standard (IDS, IdentityServer4, Keycloak, Cognito, Ping)
        //   azp       : OIDC Authorized Party (Auth0 M2M, Azure AD v2.0, Google)
        //   appid     : Azure AD v1.0 legacy
        //   cid       : Okta-specific
        // Order is preference, not security: the OAuth Client item's
        // OAuthClientIds allowlist is the actual gate on which callers are
        // accepted.
        public static string ResolveClientId(Dictionary<string, object> payload)
        {
            if (payload == null) return null;

            string[] claimNames = { "client_id", "azp", "appid", "cid" };
            foreach (var name in claimNames)
            {
                if (!payload.TryGetValue(name, out var raw)) continue;
                var value = raw?.ToString();
                if (!string.IsNullOrEmpty(value)) return value;
            }
            return null;
        }

        private static bool TryGetLong(Dictionary<string, object> payload, string key, out long value)
        {
            value = 0;
            if (!payload.TryGetValue(key, out var raw) || raw == null) return false;
            try
            {
                value = Convert.ToInt64(raw);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static IEnumerable<string> FlattenStringOrArray(object value)
        {
            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                yield return s;
                yield break;
            }

            if (value is System.Collections.IEnumerable list)
            {
                foreach (var item in list)
                {
                    if (item == null) continue;
                    var str = item.ToString();
                    if (!string.IsNullOrWhiteSpace(str)) yield return str;
                }
            }
        }
    }
}
