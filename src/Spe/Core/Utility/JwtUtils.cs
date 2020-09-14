using Sitecore.Exceptions;
using Spe.sitecore_modules.PowerShell.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Spe.Core.Extensions;
using Spe.Core.Settings.Authorization;

namespace Spe.Core.Utility
{
    public static class JwtUtils
    {
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
        }

        private static void ValidateSharedSecret(string secret)
        {
            if (string.IsNullOrWhiteSpace(secret))
                throw new SecurityException("The SPE shared secret is not set. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char random string.");

            if (double.TryParse(secret, out _))
                throw new SecurityException("The SPE shared secret is not set, or was set to a numeric value. Add a child <SharedSecret> element in the SPE <authenticationProvider> config (Spe.config) and set a secure shared secret, e.g. a 64-char random string.");

            if (secret.Length < 30) throw new SecurityException("Your SPE shared secret is not long enough. Please make it more than 30 characters for maximum security. You can set this in Spe.config on the <authenticationProvider>.");
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
            switch (algorithm)
            {
                case "HS256":
                    return new HMACSHA256(secretBytes).ComputeHash(Encoding.UTF8.GetBytes(toBeSigned));
                case "HS384":
                    return new HMACSHA384(secretBytes).ComputeHash(Encoding.UTF8.GetBytes(toBeSigned));
                case "HS512":
                    return new HMACSHA512(secretBytes).ComputeHash(Encoding.UTF8.GetBytes(toBeSigned));
                default:
                    return null;
            }
        }

        public static bool ValidateToken(string token, string authority, out string username)
        {
            username = null;
            if (string.IsNullOrEmpty(token)) return false;
            var authProvider = SpeConfigurationManager.AuthenticationProvider;
            var secret = authProvider.SharedSecret;
            ValidateSharedSecret(secret);

            var parts = token.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3) return false;

            var serializer = new JavaScriptSerializer();

            var headerJsonBase64 = parts[0];
            var decodedHeader = Decode(headerJsonBase64);
            var header = Encoding.UTF8.GetString(decodedHeader);
            var tokenHeader = serializer.Deserialize<TokenHeader>(header);

            if (tokenHeader.Typ.IsNot("JWT")) return false;

            var payloadJsonBase64 = parts[1];
            var decodedPayload = Decode(payloadJsonBase64);
            var payload = Encoding.UTF8.GetString(decodedPayload);
            var tokenPayload = serializer.Deserialize<TokenPayload>(payload);

            var nowUtc = DateTime.UtcNow;
            var expiration = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(tokenPayload.Exp);
            if (nowUtc > expiration) return false;

            if (tokenPayload.Aud.IsNot(authority)) return false;
            if (!authProvider.AllowedIssuers.Any()) return false;
            if (!authProvider.AllowedIssuers.Contains(tokenPayload.Iss)) return false;
            
            var signature = parts[2];

            var toBeSigned = $"{headerJsonBase64}.{payloadJsonBase64}";

            var hash = ComputeHash(tokenHeader.Alg, secret, toBeSigned);
            var testSignature = Convert.ToBase64String(hash).Split('=')[0]
                .Replace('+', '-').Replace('/', '_');

            if (signature != testSignature) return false;
            if (string.IsNullOrEmpty(tokenPayload.Name)) return false;
            username = tokenPayload.Name;
            return true;
        }
    }
}