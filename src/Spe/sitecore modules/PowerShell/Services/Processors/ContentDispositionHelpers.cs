using System.Text;
using System.Web;
using Sitecore.Diagnostics;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class ContentDispositionHelpers
    {
        // Strips characters that would break the Content-Disposition header parser
        // (quote/backslash terminators and CR/LF/semicolon header injection).
        public static string SanitizeFilename(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return filename;

            var sanitized = new StringBuilder(filename.Length);
            foreach (var c in filename)
            {
                if (c != '"' && c != '\\' && c != '\r' && c != '\n' && c != ';')
                {
                    sanitized.Append(c);
                }
            }
            return sanitized.ToString();
        }

        public static void AddContentHeaders(HttpContext context, string filename, long length)
        {
            Assert.ArgumentNotNull(filename, "filename");
            var response = context.Response;
            response.ClearHeaders();
            response.AddHeader("Content-Type", MimeMapping.GetMimeMapping(filename));
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + SanitizeFilename(filename) + "\"");
            response.AddHeader("Content-Length", length.ToString());
            response.AddHeader("Content-Transfer-Encoding", "binary");
        }
    }
}
