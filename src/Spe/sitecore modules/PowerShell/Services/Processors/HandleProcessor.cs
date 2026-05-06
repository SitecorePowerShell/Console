using System.Globalization;
using System.IO;
using System.Web;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Spe.Commands.Interactive.Messages;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class HandleProcessor
    {
        public static void ProcessHandle(HttpContext context, string originParam)
        {
            if (originParam.IsNullOrEmpty())
            {
                context.Response.StatusCode = 404;
                return;
            }

            // download handle
            if (!(WebUtil.GetSessionValue(originParam) is OutDownloadMessage message))
            {
                context.Response.StatusCode = 404;
                return;
            }
            WebUtil.RemoveSessionValue(originParam);
            var response = context.Response;
            response.Clear();
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + ContentDispositionHelpers.SanitizeFilename(message.Name) + "\"");
            response.ContentType = message.ContentType;
            switch (message.Content)
            {
                case FileInfo fileContent:
                    response.WriteFile(fileContent.FullName);
                    break;
                case string strContent:
                    response.Output.Write(strContent);
                    //Adam: Removing the below - to remedy Issue #863
                    //response.AddHeader("Content-Length",
                    //    strContent.Length.ToString(CultureInfo.InvariantCulture));
                    break;
                case byte[] byteContent:
                    response.OutputStream.Write(byteContent, 0, byteContent.Length);
                    response.AddHeader("Content-Length",
                        byteContent.Length.ToString(CultureInfo.InvariantCulture));
                    break;
            }

            response.End();
        }
    }
}
