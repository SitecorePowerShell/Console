using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Sitecore.Exceptions;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;

namespace Spe.Core.Settings.Authorization
{
    public class SharedSecretAuthenticationProvider : ISpeAuthenticationProviderEx
    {
        public string SharedSecret { get; set; }
        public List<string> AllowedIssuers { get; set; }
        public List<string> AllowedAudiences { get; set; }
        public bool DetailedAuthenticationErrors { get; set; }
        public int MaxTokenLifetimeSeconds { get; set; }
        public bool SuppressWarnings { get; set; }
        public int ClockSkewSeconds { get; set; }
        // Default true (secure if config is stripped). OOTB Spe.config sets this
        // to false on the SharedSecret provider so 8.x upgrades don't break
        // third-party clients that omit iat. Operators flip to true (or remove
        // the OOTB override) to enable hardened mode.
        public bool RequireIat { get; set; } = true;

        // Process-wide flag so a deployment with MaxTokenLifetimeSeconds set but
        // clients not emitting iat doesn't spam the log on every request. Resets on AppDomain recycle.
        private static int _missingIatWarningEmitted;

        public SharedSecretAuthenticationProvider()
        {
            AllowedIssuers = new List<string>();
            AllowedAudiences = new List<string>();
            ClockSkewSeconds = 30;
        }

        public bool Validate(string token, string authority, out string username)
        {
            return ValidateToken(token, authority, out username, out _);
        }

        public bool Validate(string token, string authority, out string username, out TokenValidationResult result, string sharedSecretOverride = null)
        {
            return ValidateToken(token, authority, out username, out result, sharedSecretOverride);
        }

        public class TokenHeader
        {
            public string Alg { get; set; }
            public string Typ { get; set; }
            public string Kid { get; set; }
        }

        public class TokenPayload
        {
            public string Iss { get; set; }
            public string Aud { get; set; }
            public long Exp { get; set; }
            public string Name { get; set; }
            public long Iat { get; set; }
            public long Nbf { get; set; }
            // ReSharper disable once InconsistentNaming - must match JWT claim name "client_session"
            public string Client_Session { get; set; }
        }

        // RFC 7518 Section 3.2: "A key of the same size as the hash output
        // or larger MUST be used with this algorithm."
        // Keys are UTF-8 encoded, so character count equals byte count for ASCII secrets.
        private static readonly Dictionary<string, int> MinSecretLengthByAlgorithm =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "HS256", 32 },
                { "HS384", 48 },
                { "HS512", 64 }
            };

        private bool IsValidSharedSecret(string secret, string algorithm = null)
        {
            SecurityException error = null;
            if (string.IsNullOrWhiteSpace(secret))
                error = new SecurityException("The SPE shared secret is not set. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char hex string.");

            if (error == null && double.TryParse(secret, out _))
                error = new SecurityException("The SPE shared secret is not set, or was set to a numeric value. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char hex string.");

            if (error == null && !string.IsNullOrEmpty(algorithm) && MinSecretLengthByAlgorithm.TryGetValue(algorithm, out var minLength))
            {
                if (secret.Length < minLength)
                    error = new SecurityException($"The shared secret is too short for {algorithm}. RFC 7518 requires at least {minLength} characters. Current length: {secret.Length}.");
            }
            else if (error == null && secret.Length < 32)
            {
                error = new SecurityException("The shared secret must be at least 32 characters (256 bits) for HMAC-based JWT signing.");
            }

            if (error != null)
            {
                if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason={LogSanitizer.SanitizeValue(error.Message)}");
                if (DetailedAuthenticationErrors) throw error;
                return false;
            }

            return true;
        }

        private bool IsValidTokenType(string type)
        {
            var isValid = !string.IsNullOrEmpty(type) && type.Is("JWT");
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=invalidTokenType type={LogSanitizer.SanitizeValue(type)}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Type is incorrect.");

            return false;
        }

        private bool IsValidSignature(string providedSignature, string testSignature)
        {
            var isValid = SecureCompare.FixedTimeEquals(providedSignature, testSignature);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn("[JWT] action=validationFailed reason=signatureMismatch");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token signatures do not match.");

            return false;
        }

        private bool IsValidUsername(string name)
        {
            var isValid = !string.IsNullOrEmpty(name);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn("[JWT] action=validationFailed reason=invalidUsername");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The name provided must be a valid username.");

            return false;
        }

        private static readonly HashSet<string> AllowedAlgorithms =
            new HashSet<string>(StringComparer.Ordinal) { "HS256", "HS384", "HS512" };

        private static byte[] ComputeHash(string algorithm, string secret, string toBeSigned)
        {
            if (!AllowedAlgorithms.Contains(algorithm))
            {
                PowerShellLog.Warn($"[Auth] action=algorithmRejected algorithm={LogSanitizer.SanitizeValue(algorithm)}");
                return null;
            }

            var secretBytes = Encoding.UTF8.GetBytes(secret);
            var dataBytes = Encoding.UTF8.GetBytes(toBeSigned);
            switch (algorithm)
            {
                case "HS256":
                    using (var hmac = new HMACSHA256(secretBytes)) { return hmac.ComputeHash(dataBytes); }
                case "HS384":
                    using (var hmac = new HMACSHA384(secretBytes)) { return hmac.ComputeHash(dataBytes); }
                case "HS512":
                    using (var hmac = new HMACSHA512(secretBytes)) { return hmac.ComputeHash(dataBytes); }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Extracts the kid (Key ID) claim from a JWT header without performing
        /// signature validation. Returns null if the token is malformed or has no kid.
        /// </summary>
        public static string ExtractKeyId(string token)
        {
            return ExtractHeader(token)?.Kid;
        }

        /// <summary>
        /// Extracts the alg (algorithm) claim from a JWT header without
        /// performing signature validation. The handler uses this for
        /// algorithm-based provider dispatch (HS* -> SharedSecret,
        /// RS*/ES* -> OAuth bearer). Returns null if the token is
        /// malformed.
        /// </summary>
        public static string ExtractAlgorithm(string token)
        {
            return ExtractHeader(token)?.Alg;
        }

        private static TokenHeader ExtractHeader(string token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            try
            {
                var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3) return null;

                var decodedHeader = JwtClaimValidator.Decode(parts[0]);
                var header = Encoding.UTF8.GetString(decodedHeader);
                var serializer = new JavaScriptSerializer();
                return serializer.Deserialize<TokenHeader>(header);
            }
            catch
            {
                return null;
            }
        }

        public bool ValidateToken(string token, string authority, out string username, out TokenValidationResult result, string sharedSecretOverride = null, bool skipUsernameValidation = false)
        {
            username = null;
            result = null;
            if (string.IsNullOrEmpty(token)) return false;

            var effectiveSecret = sharedSecretOverride ?? SharedSecret;

            try
            {
                // Quick rejection for null/empty/numeric secrets before parsing the token
                if (!IsValidSharedSecret(effectiveSecret)) return false;

                var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3) return false;

                var serializer = new JavaScriptSerializer();

                var headerJsonBase64 = parts[0];
                var decodedHeader = JwtClaimValidator.Decode(headerJsonBase64);
                var header = Encoding.UTF8.GetString(decodedHeader);
                var tokenHeader = serializer.Deserialize<TokenHeader>(header);

                if (!IsValidTokenType(tokenHeader.Typ)) return false;

                // Algorithm-aware secret length check per RFC 7518 Section 3.2
                if (!IsValidSharedSecret(effectiveSecret, tokenHeader.Alg)) return false;

                var payloadJsonBase64 = parts[1];
                var decodedPayload = JwtClaimValidator.Decode(payloadJsonBase64);
                var payload = Encoding.UTF8.GetString(decodedPayload);
                var tokenPayload = serializer.Deserialize<TokenPayload>(payload);

                PowerShellLog.Debug($"[JWT] action=tokenParsed issuer={LogSanitizer.SanitizeValue(tokenPayload.Iss)} audience={LogSanitizer.SanitizeValue(tokenPayload.Aud)} algorithm={LogSanitizer.SanitizeValue(tokenHeader.Alg)}");

                var signature = parts[2];
                var toBeSigned = $"{headerJsonBase64}.{payloadJsonBase64}";
                var hash = ComputeHash(tokenHeader.Alg, effectiveSecret, toBeSigned);
                if (hash == null)
                {
                    PowerShellLog.Warn("[Auth] action=authFailed reason=unsupportedAlgorithm");
                    return false;
                }
                var testSignature = Convert.ToBase64String(hash).Split('=')[0]
                    .Replace('+', '-').Replace('/', '_');

                if (!IsValidSignature(signature, testSignature)) return false;

                // Preserve historical behaviour: SharedSecret does not apply clock skew to exp.
                if (!JwtClaimValidator.IsValidExpiration(tokenPayload.Exp, 0, SuppressWarnings, DetailedAuthenticationErrors)) return false;
                if (tokenPayload.Nbf > 0 && !JwtClaimValidator.IsValidNotBefore(tokenPayload.Nbf, ClockSkewSeconds, SuppressWarnings, DetailedAuthenticationErrors)) return false;
                if (RequireIat && tokenPayload.Iat == 0)
                {
                    if (!SuppressWarnings) PowerShellLog.Warn($"[Auth] action=authFailed reason=missingIatClaim issuer={LogSanitizer.SanitizeValue(tokenPayload.Iss)}");
                    return false;
                }
                if (tokenPayload.Iat > 0 && !JwtClaimValidator.IsValidIssuedAt(tokenPayload.Iat, ClockSkewSeconds, SuppressWarnings, DetailedAuthenticationErrors)) return false;
                if (MaxTokenLifetimeSeconds > 0)
                {
                    if (tokenPayload.Iat > 0)
                    {
                        if (!JwtClaimValidator.IsValidTokenLifetime(tokenPayload.Exp, tokenPayload.Iat, MaxTokenLifetimeSeconds, SuppressWarnings, DetailedAuthenticationErrors)) return false;
                    }
                    else if (!SuppressWarnings && System.Threading.Interlocked.CompareExchange(ref _missingIatWarningEmitted, 1, 0) == 0)
                    {
                        PowerShellLog.Warn($"[Auth] action=tokenLifetimeCheckSkipped reason=missingIatClaim issuer={LogSanitizer.SanitizeValue(tokenPayload.Iss)} hint=Update client to emit iat or unset MaxTokenLifetimeSeconds.");
                    }
                }

                // Authority is implicitly added to the allowed audience list so existing deployments
                // that rely on audience == request URL keep working without explicit AllowedAudiences config.
                var effectiveAudiences = new List<string>(AllowedAudiences ?? new List<string>());
                if (!string.IsNullOrEmpty(authority)) effectiveAudiences.Add(authority);
                if (!JwtClaimValidator.IsValidAudience(tokenPayload.Aud, effectiveAudiences, SuppressWarnings, DetailedAuthenticationErrors)) return false;
                if (!JwtClaimValidator.IsValidIssuer(tokenPayload.Iss, AllowedIssuers, SuppressWarnings, DetailedAuthenticationErrors)) return false;

                if (!skipUsernameValidation)
                {
                    if (!IsValidUsername(tokenPayload.Name)) return false;
                    username = tokenPayload.Name;
                }

                result = new TokenValidationResult
                {
                    ClientSessionId = tokenPayload.Client_Session
                };

                return true;
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}