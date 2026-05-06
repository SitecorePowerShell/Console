using System;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Web;
using Spe.Core.Diagnostics;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.Utility;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class MediaProcessor
    {
        private static readonly Regex GuidRegex = new Regex(
            @"(?<id>{[a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12}})",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void ProcessMedia(HttpContext context, string serviceName, bool isUpload, Database scriptDb, string itemParam, bool unpackZip, bool skipExisting)
        {
            var request = context.Request;
            if (isUpload)
            {
                if (ZipUtils.IsZipContent(request.InputStream) && unpackZip)
                {
                    PowerShellLog.Debug("[Remoting] action=mediaExtraction destination=MediaLibrary");
                    using (var packageReader = new ZipArchive(request.InputStream))
                    {
                        itemParam = Path.GetDirectoryName(itemParam.TrimEnd('\\', '/'));
                        foreach (var zipEntry in packageReader.Entries)
                        {
                            // ZipEntry does not provide an IsDirectory or IsFile property.
                            if (!(zipEntry.FullName.EndsWith("/") && zipEntry.Name == "") && zipEntry.Length > 0)
                            {
                                ProcessMediaUpload(zipEntry.Open(), serviceName, scriptDb, $"{itemParam}/{zipEntry.FullName}",
                                    skipExisting);
                            }
                        }
                    }
                }
                else if (request.Files.AllKeys.Length > 0)
                {
                    foreach (string fileName in request.Files.Keys)
                    {
                        var file = request.Files[fileName];
                        if (file != null)
                            ProcessMediaUpload(file.InputStream, serviceName, scriptDb, $"{itemParam}/{file.FileName}",
                                skipExisting);
                    }
                }
                else
                {
                    // Buffer the input stream into a MemoryStream to ensure it is fully
                    // readable regardless of the request stream's state after IsZipContent.
                    using (var buffered = new MemoryStream())
                    {
                        request.InputStream.CopyTo(buffered);
                        buffered.Seek(0, SeekOrigin.Begin);
                        ProcessMediaUpload(buffered, serviceName, scriptDb, itemParam, skipExisting);
                    }
                }
            }
            else
            {
                ProcessMediaDownload(context, serviceName, scriptDb, itemParam);
            }
        }

        private static void ProcessMediaUpload(Stream content, string serviceName, Database db, string path, bool skipExisting = false)
        {
            path = path.Replace('\\', '/').TrimEnd('/');
            path = (path.StartsWith("/") ? path : "/" + path);
            var originalPath = path;
            var dotIndex = path.IndexOf(".", StringComparison.OrdinalIgnoreCase);
            if (dotIndex > -1)
            {
                path = path.Substring(0, dotIndex);
            }

            if (!path.StartsWith(Constants.MediaLibraryPath))
            {
                path = Constants.MediaLibraryPath + (path.StartsWith("/") ? path : "/" + path);
            }

            var mediaItem = (MediaItem)db.GetItem(path);

            if (mediaItem == null && GuidRegex.IsMatch(originalPath))
            {
                var id = GuidRegex.Match(originalPath).Value;
                mediaItem = db.GetItem(id);
            }

            if (mediaItem == null)
            {
                var fileName = Path.GetFileName(originalPath);
                var itemName = Path.GetFileNameWithoutExtension(path);
                var dirName = (Path.GetDirectoryName(path) ?? string.Empty).Replace('\\', '/');

                if (String.IsNullOrEmpty(fileName))
                {
                    PowerShellLog.Warn($"[Remoting] action=undeterminedFilename entry={LogSanitizer.SanitizeValue(originalPath)}");
                    return;
                }

                var mco = new MediaCreatorOptions
                {
                    Database = db,
                    Versioned = Settings.Media.UploadAsVersionableByDefault,
                    Destination = $"{dirName}/{itemName}",
                };

                var mc = new MediaCreator();
                using (var ms = new MemoryStream())
                {
                    content.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    mc.CreateFromStream(ms, fileName, mco);
                    if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                    {
                        PowerShellLog.Audit($"[Remoting] action=mediaUploaded service={serviceName} file={LogSanitizer.SanitizeValue(fileName)} size={ms.Length} destination=\"{LogSanitizer.SanitizeValue(mco.Destination)}\" rid={RemotingRequestContext.GetRequestId()}");
                    }
                }
            }
            else
            {
                if (skipExisting) return;

                var mediaUri = MediaUri.Parse(mediaItem);
                var media = MediaManager.GetMedia(mediaUri);

                using (var ms = new MemoryStream())
                {
                    content.CopyTo(ms);
                    ms.Seek(0, SeekOrigin.Begin);
                    var size = ms.Length;
                    using (new EditContext(mediaItem, SecurityCheck.Disable))
                    {
                        using (var mediaStream = new MediaStream(ms, media.Extension, mediaItem))
                        {
                            media.SetStream(mediaStream);
                        }
                    }
                    if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                    {
                        PowerShellLog.Audit($"[Remoting] action=mediaUpdated service={serviceName} item=\"{LogSanitizer.SanitizeValue($"{mediaItem.Name} {mediaItem.ID}")}\" size={size} rid={RemotingRequestContext.GetRequestId()}");
                    }
                }
            }
        }

        private static void ProcessMediaDownload(HttpContext context, string serviceName, Database db, string itemParam)
        {
            var indexOfDot = itemParam.IndexOf(".", StringComparison.Ordinal);
            itemParam = indexOfDot == -1 ? itemParam : itemParam.Substring(0, indexOfDot);
            itemParam = itemParam.Replace('\\', '/').TrimEnd('/');
            itemParam = itemParam.StartsWith("/") ? itemParam : $"/{itemParam}";
            itemParam = itemParam.StartsWith(ApplicationSettings.MediaLibraryPath, StringComparison.OrdinalIgnoreCase) ? itemParam : $"{ApplicationSettings.MediaLibraryPath}{itemParam}";

            var mediaItem = (MediaItem)db.GetItem(itemParam);

            if (mediaItem == null && GuidRegex.IsMatch(itemParam))
            {
                var id = GuidRegex.Match(itemParam).Value;
                mediaItem = (MediaItem)db.GetItem(id);
            }

            if (mediaItem == null)
            {
                ErrorResponseWriter.SetErrorResponse(context, 404, "The specified media is invalid.");
                return;
            }

            var mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null)
            {
                ErrorResponseWriter.SetErrorResponse(context, 404, "The specified media is invalid.");
                return;
            }

            var str = mediaItem.Extension;
            if (!str.StartsWith(".", StringComparison.InvariantCulture))
                str = "." + str;
            ContentDispositionHelpers.AddContentHeaders(context, mediaItem.Name + str, mediaItem.Size);
            if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Violations)
            {
                PowerShellLog.Audit($"[Remoting] action=mediaDownloaded service={serviceName} item=\"{LogSanitizer.SanitizeValue($"{mediaItem.Name}{str} {mediaItem.ID}")}\" size={mediaItem.Size} rid={RemotingRequestContext.GetRequestId()}");
            }
            WebUtil.TransmitStream(mediaStream, context.Response, Settings.Media.StreamBufferSize);
        }
    }
}
