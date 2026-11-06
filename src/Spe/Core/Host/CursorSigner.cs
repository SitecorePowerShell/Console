using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Spe.Core.Host
{
    /// <summary>
    /// Signs and verifies opaque cursors used by the wait-endpoint stream tee.
    /// A cursor is <c>base64url(payload).base64url(hmac)</c> where the payload encodes
    /// the session id and the next sequence offset, and the HMAC is computed with a
    /// per-app-domain random key.
    ///
    /// <para>Recycle behavior is intentional: the key is reset on every app-pool start,
    /// so any cursor minted before the recycle fails verification afterward. That's not
    /// a problem because the records the cursor pointed at, and the script session that
    /// produced them, are also gone in the same recycle - the wait endpoint resolves to
    /// <c>NotFound</c> regardless of whether the cursor would have parsed.</para>
    /// </summary>
    internal static class CursorSigner
    {
        // 32 random bytes seeded once at app-domain start. Reset on recycle.
        private static readonly byte[] _key = NewKey();

        private static byte[] NewKey()
        {
            var key = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(key);
            }
            return key;
        }

        public static string Sign(string sessionId, long offset)
        {
            // Compact payload, no whitespace; the regex on the verify side is strict.
            var payload = "{\"s\":\"" + EscapeQuotes(sessionId) + "\",\"o\":" + offset.ToString() + "}";
            var payloadB64 = Base64UrlEncode(Encoding.UTF8.GetBytes(payload));
            using (var hmac = new HMACSHA256(_key))
            {
                var sig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
                return payloadB64 + "." + Base64UrlEncode(sig);
            }
        }

        public static bool TryVerify(string cursor, string expectedSessionId, out long offset)
        {
            offset = 0;
            if (string.IsNullOrEmpty(cursor)) return false;

            var dot = cursor.IndexOf('.');
            if (dot <= 0 || dot == cursor.Length - 1) return false;

            var payloadB64 = cursor.Substring(0, dot);
            var sigB64     = cursor.Substring(dot + 1);

            byte[] expectedSig;
            using (var hmac = new HMACSHA256(_key))
            {
                expectedSig = hmac.ComputeHash(Encoding.UTF8.GetBytes(payloadB64));
            }

            if (!TryBase64UrlDecode(sigB64, out var actualSig)) return false;
            if (!ConstantTimeEquals(expectedSig, actualSig))   return false;

            if (!TryBase64UrlDecode(payloadB64, out var payloadBytes)) return false;
            string payload;
            try { payload = Encoding.UTF8.GetString(payloadBytes); }
            catch { return false; }

            var match = Regex.Match(payload, "^\\{\"s\":\"([^\"\\\\]+)\",\"o\":(\\d+)\\}$");
            if (!match.Success) return false;
            if (!string.Equals(match.Groups[1].Value, expectedSessionId, StringComparison.Ordinal)) return false;
            return long.TryParse(match.Groups[2].Value, out offset);
        }

        private static string EscapeQuotes(string s) => (s ?? string.Empty).Replace("\"", "\\\"");

        private static string Base64UrlEncode(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }

        private static bool TryBase64UrlDecode(string s, out byte[] bytes)
        {
            bytes = null;
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                var padded = s.Replace('-', '+').Replace('_', '/');
                switch (padded.Length % 4)
                {
                    case 2: padded += "=="; break;
                    case 3: padded += "=";  break;
                    case 1: return false;
                }
                bytes = Convert.FromBase64String(padded);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
