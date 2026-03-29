using System.Runtime.CompilerServices;

namespace Spe.Core.Settings.Authorization
{
    internal static class SecureCompare
    {
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
