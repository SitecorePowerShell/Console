using System;
using System.IO;
using System.Web;
using Spe.Core.Diagnostics;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class FileProcessor
    {
        public static void ProcessFile(HttpContext context, string serviceName, bool isUpload, string originParam, string pathParam)
        {
            if (isUpload)
            {
                ProcessFileUpload(context, serviceName, originParam, pathParam);
            }
            else
            {
                ProcessFileDownload(context, serviceName, originParam, pathParam);
            }
        }

        // Single entry point for file uploads and downloads. Resolves the path
        // through the alias-or-allowlist policy and writes a 403 + audit on
        // rejection. Returns false when the caller should stop processing.
        private static bool TryResolveFilePath(HttpContext context, string serviceName, string originParam, string pathParam, out string file)
        {
            file = FilePathResolver.GetPathFromParameters(originParam, pathParam, out var rejectReason);
            if (!string.IsNullOrEmpty(file))
            {
                return true;
            }

            var ip = RemotingRequestContext.GetIp(context.Request);
            PowerShellLog.Audit($"[Remoting] action=fileAccessDenied reason={rejectReason ?? "unknownOrigin"} service={serviceName} ip={ip} origin={LogSanitizer.SanitizeValue(originParam ?? string.Empty)} path=\"{LogSanitizer.SanitizeValue(pathParam ?? string.Empty)}\" rid={RemotingRequestContext.GetRequestId()}");
            ErrorResponseWriter.SetErrorResponse(context, 403, "Path is not allowed.");
            return false;
        }

        private static void ProcessFileUpload(HttpContext context, string serviceName, string originParam, string pathParam)
        {
            if (!TryResolveFilePath(context, serviceName, originParam, pathParam, out var file))
            {
                return;
            }

            try
            {
                var fileInfo = new FileInfo(file);
                if (!fileInfo.Exists)
                {
                    fileInfo.Directory?.Create();
                }
                using (var output = fileInfo.OpenWrite())
                {
                    using (var input = context.Request.InputStream)
                    {
                        input.CopyTo(output);
                    }
                }

                fileInfo.Refresh();
                if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                {
                    PowerShellLog.Audit($"[Remoting] action=fileUploaded service={serviceName} file={LogSanitizer.SanitizeValue(fileInfo.Name)} size={fileInfo.Length} path=\"{LogSanitizer.SanitizeValue(file)}\" rid={RemotingRequestContext.GetRequestId()}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                PowerShellLog.Error($"[Remoting] action=writePermissionDenied service={serviceName} path=\"{LogSanitizer.SanitizeValue(file)}\"", ex);
                ErrorResponseWriter.SetErrorResponse(context, 403, "Write access denied to the target path.");
            }
            catch (ArgumentException ex)
            {
                PowerShellLog.Error($"[Remoting] action=invalidFilePath service={serviceName} path=\"{LogSanitizer.SanitizeValue(file)}\"", ex);
                ErrorResponseWriter.SetErrorResponse(context, 400, "The specified path contains invalid characters.");
            }
        }

        private static void ProcessFileDownload(HttpContext context, string serviceName, string originParam, string pathParam)
        {
            if (!TryResolveFilePath(context, serviceName, originParam, pathParam, out var file))
            {
                return;
            }

            if (!File.Exists(file))
            {
                ErrorResponseWriter.SetErrorResponse(context, 404, "The specified path is invalid.");
                return;
            }

            var fileInfo = new FileInfo(file);
            ContentDispositionHelpers.AddContentHeaders(context, fileInfo.Name, fileInfo.Length);
            if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Violations)
            {
                PowerShellLog.Audit($"[Remoting] action=fileDownloaded service={serviceName} file={LogSanitizer.SanitizeValue(fileInfo.Name)} size={fileInfo.Length} path=\"{LogSanitizer.SanitizeValue(file)}\" rid={RemotingRequestContext.GetRequestId()}");
            }
            try
            {
                context.Response.TransmitFile(file);
            }
            catch (IOException ex)
            {
                // ex.Message can carry the absolute server path in the file-
                // share / locked-file cases ("The process cannot access the
                // file 'C:\inetpub\...'..."). Audit log keeps the detail;
                // client gets a generic phrase unless DetailedErrors is on.
                PowerShellLog.Error($"[Remoting] action=transmitFileFailed file=\"{LogSanitizer.SanitizeValue(file)}\" rid={RemotingRequestContext.GetRequestId()}", ex);
                context.Response.StatusCode = 500;
                context.Response.StatusDescription = WebServiceSettings.DetailedErrors
                    ? ex.Message
                    : "Read error";
            }
        }
    }
}
