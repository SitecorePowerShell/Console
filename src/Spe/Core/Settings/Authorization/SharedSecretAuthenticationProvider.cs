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
        private const int ClockSkewSeconds = 30;

        public string SharedSecret { get; set; }
        public List<string> AllowedIssuers { get; set; }
        public List<string> AllowedAudiences { get; set; }
        public bool DetailedAuthenticationErrors { get; set; }
        public int MaxTokenLifetimeSeconds { get; set; }
        public bool SuppressWarnings { get; set; }

        public SharedSecretAuthenticationProvider()
        {
            AllowedIssuers = new List<string>();
            AllowedAudiences = new List<string>();
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

        private bool IsValidAudience(string audience, string authority)
        {
            PowerShellLog.Debug($"[JWT] action=audienceCheck audience={LogSanitizer.SanitizeValue(audience)} authority={LogSanitizer.SanitizeValue(authority)}");
            var isValid = !string.IsNullOrEmpty(audience) &&
                          (audience.Is(authority) ||
                           AllowedAudiences.Any() &&
                           AllowedAudiences.Contains(audience));
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=audienceNotAllowed audience={LogSanitizer.SanitizeValue(audience)} authority={LogSanitizer.SanitizeValue(authority)}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Audience is not allowed.");

            return false;
        }

        private bool IsValidIssuer(string issuer)
        {
            var isValid = !string.IsNullOrEmpty(issuer) &&
                          AllowedIssuers.Any() &&
                          AllowedIssuers.Contains(issuer);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=issuerNotAllowed issuer={LogSanitizer.SanitizeValue(issuer)}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Issuer is not allowed.");

            return false;
        }

        private bool IsValidExpiration(long expiration)
        {
            var nowUtc = DateTime.UtcNow;
            var expireUtc = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(expiration);
            var isValid = nowUtc < expireUtc;
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=tokenExpired expiry={expireUtc:O}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Expiration has passed.");

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

        private bool IsValidNotBefore(long nbf)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var nbfUtc = epoch.AddSeconds(nbf);
            var isValid = DateTime.UtcNow >= nbfUtc.AddSeconds(-ClockSkewSeconds);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=notYetValid nbf={nbfUtc:O}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token is not yet valid (nbf claim).");

            return false;
        }

        private bool IsValidIssuedAt(long iat)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var iatUtc = epoch.AddSeconds(iat);
            var isValid = DateTime.UtcNow >= iatUtc.AddSeconds(-ClockSkewSeconds);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=issuedInFuture iat={iatUtc:O}");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token issued-at time is in the future (iat claim).");

            return false;
        }

        private bool IsValidTokenLifetime(long exp, long iat)
        {
            var lifetime = exp - iat;
            var isValid = lifetime <= MaxTokenLifetimeSeconds;
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"[JWT] action=validationFailed reason=lifetimeExceeded lifetime={lifetime}s maximum={MaxTokenLifetimeSeconds}s");
            if (DetailedAuthenticationErrors)
                throw new SecurityException($"The Token lifetime ({lifetime}s) exceeds the maximum allowed ({MaxTokenLifetimeSeconds}s).");

            return false;
        }

        private static byte[] Decode(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException(nameof(input));

            var output = input;
            output = output.Replace('-', '+'); // 62nd char of encoding
            output = output.Replace('_', '/'); // 63rd char of encoding
            switch (output.Length % 4) // Pad with trailing '='s
            {
                case 0:
                    break; // No pad chars in this case
                case 2:
                    output += "==";
                    break; // Two pad chars
                case 3:
                    output += "=";
                    break; // One pad char
                default:
                    throw new FormatException("Illegal base64url string.");
            }
            var converted = Convert.FromBase64String(output); // Standard base64 decoder
            return converted;
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
            if (string.IsNullOrEmpty(token)) return null;

            try
            {
                var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3) return null;

                var decodedHeader = Decode(parts[0]);
                var header = Encoding.UTF8.GetString(decodedHeader);
                var serializer = new JavaScriptSerializer();
                var tokenHeader = serializer.Deserialize<TokenHeader>(header);
                return tokenHeader?.Kid;
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
                var decodedHeader = Decode(headerJsonBase64);
                var header = Encoding.UTF8.GetString(decodedHeader);
                var tokenHeader = serializer.Deserialize<TokenHeader>(header);

                if (!IsValidTokenType(tokenHeader.Typ)) return false;

                // Algorithm-aware secret length check per RFC 7518 Section 3.2
                if (!IsValidSharedSecret(effectiveSecret, tokenHeader.Alg)) return false;

                var payloadJsonBase64 = parts[1];
                var decodedPayload = Decode(payloadJsonBase64);
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

                if (!IsValidExpiration(tokenPayload.Exp)) return false;
                if (tokenPayload.Nbf > 0 && !IsValidNotBefore(tokenPayload.Nbf)) return false;
                if (tokenPayload.Iat > 0 && !IsValidIssuedAt(tokenPayload.Iat)) return false;
                if (MaxTokenLifetimeSeconds > 0 && tokenPayload.Iat > 0 && !IsValidTokenLifetime(tokenPayload.Exp, tokenPayload.Iat)) return false;
                if (!IsValidAudience(tokenPayload.Aud, authority)) return false;
                if (!IsValidIssuer(tokenPayload.Iss)) return false;

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