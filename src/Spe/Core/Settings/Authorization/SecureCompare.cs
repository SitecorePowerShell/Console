using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Spe.Core.Settings.Authorization
{
    internal static class SecureCompare
    {
        /// <summary>
        /// Compares two strings in constant time using SHA256 to normalize inputs
        /// to fixed-length hashes before comparison. This prevents leaking the
        /// length of the expected value via timing differences.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool FixedTimeSecretEquals(string a, string b)
        {
            if (a == null || b == null) return false;

            using (var sha = SHA256.Create())
            {
                var hashA = sha.ComputeHash(Encoding.UTF8.GetBytes(a));
                var hashB = sha.ComputeHash(Encoding.UTF8.GetBytes(b));

                return FixedTimeEquals(
                    Convert.ToBase64String(hashA),
                    Convert.ToBase64String(hashB));
            }
        }

        /// <summary>
        /// Compares two strings in constant time to prevent timing attacks.
        /// Returns true only if both strings are non-null, equal length, and identical.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static bool FixedTimeEquals(string a, string b)
        {
            if (a == null || b == null) return false;
            if (a.Length != b.Length) return false;

            var result = 0;
            for (var i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
