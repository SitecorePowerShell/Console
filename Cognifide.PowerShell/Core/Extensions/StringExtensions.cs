using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class StringExtensions
    {
        public static bool Is(this string value, string compare)
        {
            return String.Compare(value, compare, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static bool IsNot(this string value, string compare)
        {
            return !Is(value, compare);
        }

        public static bool IsSubstringOf(this string subString, string value)
        {
            return value.IndexOf(subString, StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static bool HasSubstring(this string value, object subString)
        {
            subString = subString ?? string.Empty;
            return (value ?? string.Empty).IndexOf(subString.ToString(), StringComparison.OrdinalIgnoreCase) != -1;
        }

        public static bool Contains(this string value, Enum option)
        {
            return (value ?? string.Empty).IndexOf(option.ToString(), StringComparison.OrdinalIgnoreCase) != -1;
        }


        public static string EllipsisString(this string rawString, int maxLength = 30, char delimiter = '\\')
        {
            maxLength -= 3; //account for delimiter spacing

            if (rawString.Length <= maxLength)
            {
                return rawString;
            }

            var parts = rawString.Split(delimiter).ToList();
            var loops = 0;
            while (loops++ < 100)
            {
                var removed = parts.Count/2;
                parts.RemoveAt(removed);
                if (parts.Count == 1)
                {
                    return parts.First();
                }

                parts.Insert(removed, "...");
                var final = string.Join(delimiter.ToString(), parts);
                if (final.Length < maxLength)
                {
                    return final;
                }
                parts.RemoveAt(removed);
            }

            return rawString.Split(delimiter).ToList().Last();
        }

        public static string RemoveHtmlTags(this string value)
        {
            return !String.IsNullOrEmpty(value) ? Regex.Replace(value, "<.*?>", String.Empty) : value;
        }
    }
}