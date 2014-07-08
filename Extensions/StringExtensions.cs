using System;

namespace Cognifide.PowerShell.Extensions
{
    public static class StringExtensions
    {
        public static bool Is(this string value, string compare)
        {
            return String.Compare(value, compare, StringComparison.CurrentCultureIgnoreCase) == 0;
        }
    }
}