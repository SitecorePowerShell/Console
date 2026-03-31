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

        public bool Validate(string token, string authority, out string username, out TokenValidationResult result)
        {
            return ValidateToken(token, authority, out username, out result);
        }

        public class TokenHeader
        {
            public string Alg { get; set; }
            public string Typ { get; set; }
        }

        public class TokenPayload
        {
            public string Iss { get; set; }
            public string Aud { get; set; }
            public long Exp { get; set; }
            public string Name { get; set; }
            public string Scope { get; set; }
            public long Iat { get; set; }
            public long Nbf { get; set; }
            // ReSharper disable once InconsistentNaming -- must match JWT claim name "client_session"
            public string Client_Session { get; set; }
        }

        private bool IsValidSharedSecret()
        {
            SecurityException error = null;
            var isValid = true;
            if (string.IsNullOrWhiteSpace(SharedSecret))
                error = new SecurityException("The SPE shared secret is not set. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char random string.");

            if (double.TryParse(SharedSecret, out _))
                error = new SecurityException("The SPE shared secret is not set, or was set to a numeric value. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char random string.");

            if (SharedSecret.Length < 30)
                error = new SecurityException("Your SPE shared secret is not long enough. Please make it more than 30 characters for maximum security. You can set this in Spe.config on the <authenticationProvider>.");

            if (error != null)
            {
                isValid = false;
                if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: {error.Message}");
            }

            if (DetailedAuthenticationErrors && error != null)
            {
                throw error;
            }

            if (isValid) return true;

            return false;
        }

        private bool IsValidTokenType(string type)
        {
            var isValid = !string.IsNullOrEmpty(type) && type.Is("JWT");
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: token type '{type}' is not JWT.");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Type is incorrect.");

            return false;
        }

        private bool IsValidAudience(string audience, string authority)
        {
            PowerShellLog.Debug($"The current audience is {audience} and the current authority is {authority}.");
            var isValid = !string.IsNullOrEmpty(audience) &&
                          (audience.Is(authority) ||
                           AllowedAudiences.Any() &&
                           AllowedAudiences.Contains(audience));
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: audience '{audience}' is not allowed (authority: '{authority}').");
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

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: issuer '{issuer}' is not allowed.");
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

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: token expired at {expireUtc:O}.");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token Expiration has passed.");

            return false;
        }

        private bool IsValidSignature(string providedSignature, string testSignature)
        {
            var isValid = SecureCompare.FixedTimeEquals(providedSignature, testSignature);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn("JWT validation failed: token signature does not match.");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token signatures do not match.");

            return false;
        }

        private bool IsValidUsername(string name)
        {
            var isValid = !string.IsNullOrEmpty(name);
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn("JWT validation failed: token does not contain a valid username.");
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

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: token is not yet valid (nbf: {nbfUtc:O}).");
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

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: token issued-at is in the future (iat: {iatUtc:O}).");
            if (DetailedAuthenticationErrors)
                throw new SecurityException("The Token issued-at time is in the future (iat claim).");

            return false;
        }

        private bool IsValidTokenLifetime(long exp, long iat)
        {
            var lifetime = exp - iat;
            var isValid = lifetime <= MaxTokenLifetimeSeconds;
            if (isValid) return true;

            if (!SuppressWarnings) PowerShellLog.Warn($"JWT validation failed: token lifetime {lifetime}s exceeds maximum {MaxTokenLifetimeSeconds}s.");
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

        private static byte[] ComputeHash(string algorithm, string secret, string toBeSigned)
        {
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

        public bool ValidateToken(string token, string authority, out string username, out TokenValidationResult result)
        {
            username = null;
            result = null;
            if (string.IsNullOrEmpty(token)) return false;

            try
            {
                if (!IsValidSharedSecret()) return false;

                var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3) return false;

                var serializer = new JavaScriptSerializer();

                var headerJsonBase64 = parts[0];
                var decodedHeader = Decode(headerJsonBase64);
                var header = Encoding.UTF8.GetString(decodedHeader);
                var tokenHeader = serializer.Deserialize<TokenHeader>(header);

                if (!IsValidTokenType(tokenHeader.Typ)) return false;

                var payloadJsonBase64 = parts[1];
                var decodedPayload = Decode(payloadJsonBase64);
                var payload = Encoding.UTF8.GetString(decodedPayload);
                var tokenPayload = serializer.Deserialize<TokenPayload>(payload);

                PowerShellLog.Debug($"JWT validation: issuer='{tokenPayload.Iss}', audience='{tokenPayload.Aud}', algorithm='{tokenHeader.Alg}'.");

                if (!IsValidExpiration(tokenPayload.Exp)) return false;
                if (tokenPayload.Nbf > 0 && !IsValidNotBefore(tokenPayload.Nbf)) return false;
                if (tokenPayload.Iat > 0 && !IsValidIssuedAt(tokenPayload.Iat)) return false;
                if (MaxTokenLifetimeSeconds > 0 && tokenPayload.Iat > 0 && !IsValidTokenLifetime(tokenPayload.Exp, tokenPayload.Iat)) return false;
                if (!IsValidAudience(tokenPayload.Aud, authority)) return false;
                if (!IsValidIssuer(tokenPayload.Iss)) return false;

                var signature = parts[2];

                var toBeSigned = $"{headerJsonBase64}.{payloadJsonBase64}";

                var hash = ComputeHash(tokenHeader.Alg, SharedSecret, toBeSigned);
                var testSignature = Convert.ToBase64String(hash).Split('=')[0]
                    .Replace('+', '-').Replace('/', '_');

                if (!IsValidSignature(signature, testSignature)) return false;
                if (!IsValidUsername(tokenPayload.Name)) return false;
                username = tokenPayload.Name;

                result = new TokenValidationResult
                {
                    Scope = tokenPayload.Scope,
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