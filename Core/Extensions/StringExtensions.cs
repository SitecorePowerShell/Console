using System;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool Is(this string value, string compare)
        {
            return String.Compare(value, compare, StringComparison.CurrentCultureIgnoreCase) == 0;
        }

        public static bool IsNot(this string value, string compare)
        {
            return !Is(value, compare);
        }
    }
}