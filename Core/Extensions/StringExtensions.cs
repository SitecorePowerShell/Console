using System;
using System.Collections.Generic;
using System.Linq;

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

        public static string EllipsisString(this string rawString, int maxLength = 30, char delimiter = '\\')
        {
            maxLength -= 3; //account for delimiter spacing

            if (rawString.Length <= maxLength)
            {
                return rawString;
            }

            string final = rawString;
            List<string> parts;

            parts = rawString.Split(delimiter).ToList();
            int loops = 0;
            while (loops++ < 100)
            {
                int removed = parts.Count / 2;
                parts.RemoveAt(removed);
                if (parts.Count == 1)
                {
                    return parts.First();
                }

                parts.Insert(removed, "...");
                final = string.Join(delimiter.ToString(), parts);
                if (final.Length < maxLength)
                {
                    return final;
                }
                parts.RemoveAt(removed);
            }

            return rawString.Split(delimiter).ToList().Last();
        }
    }
}