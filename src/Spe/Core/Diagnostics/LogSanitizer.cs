using System;
using System.Linq;
using System.Web;

namespace Spe.Core.Diagnostics
{
    /// <summary>
    /// Sanitizes external input before interpolation into structured log messages.
    /// Prevents log injection by encoding characters that the ToJson key=value parser uses as delimiters.
    /// </summary>
    internal static class LogSanitizer
    {
        /// <summary>
        /// Encodes characters that could inject fake key=value pairs into log messages.
        /// Covers: equals (field delimiter), space (value terminator), newline/carriage return/tab (line injection).
        /// </summary>
        public static string SanitizeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "(empty)";
            if (value.IndexOfAny(DangerousChars) < 0) return value;

            return value
                .Replace("=", "%3D")
                .Replace(" ", "%20")
                .Replace("\n", "%0A")
                .Replace("\r", "%0D")
                .Replace("\t", "%09");
        }

        /// <summary>
        /// Redacts sensitive query string parameters from a URL before logging.
        /// </summary>
        public static string RedactUrl(Uri url)
        {
            if (url == null) return "(null)";

            var query = url.Query;
            if (string.IsNullOrEmpty(query)) return url.ToString();

            var queryParams = HttpUtility.ParseQueryString(query);
            var keysToRedact = queryParams.AllKeys.Where(IsSensitiveParam).ToArray();
            foreach (var key in keysToRedact)
            {
                queryParams[key] = "***REDACTED***";
            }

            var builder = new UriBuilder(url) { Query = queryParams.ToString() };
            return builder.Uri.ToString();
        }

        private static bool IsSensitiveParam(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;
            var lower = key.ToLowerInvariant();
            return lower == "password" || lower == "username" || lower == "credential"
                || lower == "secret" || lower == "token";
        }

        private static readonly char[] DangerousChars = { '=', ' ', '\n', '\r', '\t' };
    }
}
