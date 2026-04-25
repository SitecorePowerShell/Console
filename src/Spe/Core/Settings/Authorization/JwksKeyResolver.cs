using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    internal static class JwksKeyResolver
    {
        // 1 MB upper bound on a JWKS response - a healthy keyset is well
        // under 10 KB; anything past 1 MB is either misconfiguration or a
        // hostile / compromised IdP and is rejected.
        private const int MaxResponseBytes = 1024 * 1024;

        // After a failed fetch, suppress further attempts for this many
        // seconds. Bounded constant (not operator-tunable) to avoid long
        // lockouts from misconfig.
        private const int NegativeCacheSeconds = 15;

        private static readonly ConcurrentDictionary<string, CacheEntry> Cache =
            new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<string, DateTime> NegativeCache =
            new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);

        private static readonly Lazy<HttpClient> Http = new Lazy<HttpClient>(() =>
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            return client;
        });

        // Pure-function URI gate. HTTPS always allowed. http is rejected by
        // default; setting allowLoopbackHttp=true permits http://localhost,
        // 127.0.0.1, and [::1] for development. The out-param tells the
        // fetch path whether to emit the loopback-http audit warn.
        public static bool IsJwksUriAcceptable(string uri, bool allowLoopbackHttp, out bool isLoopbackHttp)
        {
            isLoopbackHttp = false;
            if (string.IsNullOrWhiteSpace(uri)) return false;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsed)) return false;
            if (parsed.Scheme == Uri.UriSchemeHttps) return true;
            if (parsed.Scheme == Uri.UriSchemeHttp && allowLoopbackHttp && parsed.IsLoopback)
            {
                isLoopbackHttp = true;
                return true;
            }
            return false;
        }

        private sealed class CacheEntry
        {
            public DateTime ExpiresUtc;
            public Dictionary<string, JsonWebKey> KeysByKid;
        }

        public sealed class JsonWebKey
        {
            public string Kty { get; set; }
            public string Alg { get; set; }
            public string Kid { get; set; }
            public string Use { get; set; }
            public string N { get; set; }
            public string E { get; set; }
            public string Crv { get; set; }
            public string X { get; set; }
            public string Y { get; set; }
            public string[] X5c { get; set; }
        }

        private sealed class JwksDocument
        {
            public JsonWebKey[] Keys { get; set; }
        }

        public static JsonWebKey GetKey(string jwksUri, string kid, int cacheSeconds, bool allowLoopbackHttp)
        {
            if (string.IsNullOrWhiteSpace(jwksUri) || string.IsNullOrWhiteSpace(kid)) return null;

            if (!IsJwksUriAcceptable(jwksUri, allowLoopbackHttp, out var isLoopbackHttp))
            {
                PowerShellLog.Warn($"[JWKS] action=jwksUriRejected reason=schemeNotAllowed uri={LogSanitizer.SanitizeValue(jwksUri)}");
                return null;
            }

            if (NegativeCache.TryGetValue(jwksUri, out var negativeUntil) && negativeUntil > DateTime.UtcNow)
            {
                return null;
            }

            var entry = Cache.TryGetValue(jwksUri, out var cached) && cached.ExpiresUtc > DateTime.UtcNow
                ? cached
                : Refresh(jwksUri, cacheSeconds, isLoopbackHttp);

            if (entry == null) return null;
            if (entry.KeysByKid.TryGetValue(kid, out var key)) return key;

            // Unknown kid against a cached document: try one forced refresh in case keys rotated.
            entry = Refresh(jwksUri, cacheSeconds, isLoopbackHttp);
            if (entry != null && entry.KeysByKid.TryGetValue(kid, out key)) return key;

            PowerShellLog.Warn($"[JWKS] action=keyNotFound kid={LogSanitizer.SanitizeValue(kid)} uri={LogSanitizer.SanitizeValue(jwksUri)}");
            return null;
        }

        private static CacheEntry Refresh(string jwksUri, int cacheSeconds, bool isLoopbackHttp)
        {
            try
            {
                if (isLoopbackHttp)
                {
                    PowerShellLog.Warn($"[JWKS] action=loopbackHttpFetch uri={LogSanitizer.SanitizeValue(jwksUri)}");
                }

                var response = Http.Value.GetAsync(jwksUri).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    RecordFailure(jwksUri);
                    PowerShellLog.Warn($"[JWKS] action=fetchFailed uri={LogSanitizer.SanitizeValue(jwksUri)} status={(int)response.StatusCode}");
                    return null;
                }

                var contentLength = response.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > MaxResponseBytes)
                {
                    RecordFailure(jwksUri);
                    PowerShellLog.Warn($"[JWKS] action=fetchFailed reason=responseTooLarge uri={LogSanitizer.SanitizeValue(jwksUri)} size={contentLength.Value} max={MaxResponseBytes}");
                    return null;
                }

                var body = ReadCapped(response, MaxResponseBytes);
                if (body == null)
                {
                    RecordFailure(jwksUri);
                    PowerShellLog.Warn($"[JWKS] action=fetchFailed reason=responseTooLarge uri={LogSanitizer.SanitizeValue(jwksUri)} max={MaxResponseBytes}");
                    return null;
                }

                var doc = new JavaScriptSerializer().Deserialize<JwksDocument>(body);
                if (doc?.Keys == null || doc.Keys.Length == 0)
                {
                    RecordFailure(jwksUri);
                    PowerShellLog.Warn($"[JWKS] action=fetchFailed reason=emptyKeyset uri={LogSanitizer.SanitizeValue(jwksUri)}");
                    return null;
                }

                var entry = new CacheEntry
                {
                    ExpiresUtc = DateTime.UtcNow.AddSeconds(cacheSeconds > 0 ? cacheSeconds : 600),
                    KeysByKid = doc.Keys
                        .Where(k => !string.IsNullOrEmpty(k?.Kid))
                        .GroupBy(k => k.Kid, StringComparer.Ordinal)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal)
                };

                Cache[jwksUri] = entry;
                NegativeCache.TryRemove(jwksUri, out _);
                return entry;
            }
            catch (Exception ex)
            {
                RecordFailure(jwksUri);
                PowerShellLog.Warn($"[JWKS] action=fetchFailed uri={LogSanitizer.SanitizeValue(jwksUri)} error={LogSanitizer.SanitizeValue(ex.GetType().Name)}");
                return null;
            }
        }

        private static void RecordFailure(string jwksUri)
        {
            NegativeCache[jwksUri] = DateTime.UtcNow.AddSeconds(NegativeCacheSeconds);
        }

        private static string ReadCapped(HttpResponseMessage response, int maxBytes)
        {
            using (var stream = response.Content.ReadAsStreamAsync().GetAwaiter().GetResult())
            using (var ms = new MemoryStream())
            {
                var buffer = new byte[8192];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (ms.Length + read > maxBytes) return null;
                    ms.Write(buffer, 0, read);
                }
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // Test seam: lets the unit suite reset state between cases.
        internal static void ClearCaches()
        {
            Cache.Clear();
            NegativeCache.Clear();
        }

        public static bool VerifyRsa(JsonWebKey key, string algorithm, byte[] signedBytes, byte[] signatureBytes)
        {
            if (key == null || !string.Equals(key.Kty, "RSA", StringComparison.Ordinal)) return false;

            using (var rsa = new RSACryptoServiceProvider())
            {
                try
                {
                    rsa.ImportParameters(new RSAParameters
                    {
                        Modulus = JwtClaimValidator.Decode(key.N),
                        Exponent = JwtClaimValidator.Decode(key.E)
                    });
                }
                catch
                {
                    return false;
                }

                HashAlgorithmName hash;
                switch (algorithm)
                {
                    case "RS256": hash = HashAlgorithmName.SHA256; break;
                    case "RS384": hash = HashAlgorithmName.SHA384; break;
                    case "RS512": hash = HashAlgorithmName.SHA512; break;
                    default: return false;
                }

                return rsa.VerifyData(signedBytes, signatureBytes, hash, RSASignaturePadding.Pkcs1);
            }
        }

        public static bool VerifyEcdsa(JsonWebKey key, string algorithm, byte[] signedBytes, byte[] signatureBytes)
        {
            if (key == null || !string.Equals(key.Kty, "EC", StringComparison.Ordinal)) return false;

            ECCurve curve;
            HashAlgorithmName hash;
            switch (algorithm)
            {
                case "ES256":
                    curve = ECCurve.NamedCurves.nistP256;
                    hash = HashAlgorithmName.SHA256;
                    break;
                case "ES384":
                    curve = ECCurve.NamedCurves.nistP384;
                    hash = HashAlgorithmName.SHA384;
                    break;
                case "ES512":
                    curve = ECCurve.NamedCurves.nistP521;
                    hash = HashAlgorithmName.SHA512;
                    break;
                default:
                    return false;
            }

            try
            {
                using (var ecdsa = ECDsa.Create(new ECParameters
                {
                    Curve = curve,
                    Q = new ECPoint
                    {
                        X = JwtClaimValidator.Decode(key.X),
                        Y = JwtClaimValidator.Decode(key.Y)
                    }
                }))
                {
                    return ecdsa.VerifyData(signedBytes, signatureBytes, hash);
                }
            }
            catch
            {
                return false;
            }
        }

        // JOSE signatures are base64url; strip padding and switch alphabet back for decoding.
        public static byte[] DecodeSignature(string signature)
        {
            return JwtClaimValidator.Decode(signature);
        }

        public static byte[] GetSignedBytes(string headerAndPayload)
        {
            return Encoding.ASCII.GetBytes(headerAndPayload);
        }
    }
}
