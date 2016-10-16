using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Web;
using System.Web.SessionState;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.Security.Authentication;
using Sitecore.SecurityModel;
using Sitecore.Web;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    ///     The handler allows for execution of scripts stored within Script Library
    ///     it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler, IRequiresSessionState
    {
        private SortedDictionary<string, SortedDictionary<string, ApiScript>> apiScripts;
        private static Dictionary<string,string> apiVersionToServiceMapping = new Dictionary<string, string>()
        {
            { "POST/file" , WebServiceSettings.ServiceFileUpload },
            { "POST/media" , WebServiceSettings.ServiceMediaUpload },
            { "GET/1" , WebServiceSettings.ServiceRestfulv1 },
            { "GET/2" , WebServiceSettings.ServiceRestfulv2 },
            { "GET/file" , WebServiceSettings.ServiceFileDownload },
            { "GET/media" , WebServiceSettings.ServiceMediaDownload },
            { "GET/handle" , WebServiceSettings.ServiceHandleDownload },
        };
        public void ProcessRequest(HttpContext context)
        {
            var request = HttpContext.Current.Request;
            var userName = request.Params.Get("user");
            var password = request.Params.Get("password");
            var itemParam = request.Params.Get("script");
            var pathParam = request.Params.Get("path");
            var originParam = request.Params.Get("scriptDb");
            var apiVersion = request.Params.Get("apiVersion");
            var serviceMappingKey = request.HttpMethod + "/" + apiVersion;
            var isUpload = request.HttpMethod.Is("POST") && request.InputStream.Length > 0;
            var unpackZip = request.Params.Get("skipunpack").IsNot("true");
            var skipExisting = request.Params.Get("skipexisting").Is("true");
            var serviceName = apiVersionToServiceMapping.ContainsKey(serviceMappingKey)
                ? apiVersionToServiceMapping[serviceMappingKey]
                : string.Empty;

            // verify that the service is enabled
            if (!CheckServiceEnabled(apiVersion, request.HttpMethod))
            {
                PowerShellLog.Error($"Attempt to call the {apiVersion} service failed as it is not enabled.");
                return;
            }

            // verify that the user is authorized to access the end point
            var authUserName = string.IsNullOrEmpty(userName) ? Context.User.Name : userName;
            if (!ServiceAuthorizationManager.IsUserAuthorized(serviceName, authUserName, false))
            {
                HttpContext.Current.Response.StatusCode = 401;
                PowerShellLog.Error($"Attempt to call the '{apiVersion}' service failed as user '{userName}' was not authorized.");
                return;
            }

            // login user if specified explicitly
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                var account = new AccountIdentity(userName);
                AuthenticationManager.Login(account.Name, password, false);
            }

            var isAuthenticated = Context.IsLoggedIn;
            var useContextDatabase = apiVersion.Is("file") || apiVersion.Is("handle") || !isAuthenticated || string.IsNullOrEmpty(originParam) || originParam.Is("current");
            var scriptDb = useContextDatabase ? Context.Database : Database.GetDatabase(originParam);
            var dbName = scriptDb.Name;

            if (!CheckServiceAuthentication(apiVersion, isAuthenticated))
            {
                PowerShellLog.Error($"Attempt to call the {apiVersion} service failed as - user not logged in, authentication failed or no credentials provided.");
                return;
            }

            if (isUpload)
            {
                switch (apiVersion)
                {
                    case "media":
                        if (ZipUtils.IsZipContent(request.InputStream) && unpackZip)
                        {
                            PowerShellLog.Debug("The uploaded asset will be extracted to Media Library.");
                            using (var packageReader = new Sitecore.Zip.ZipReader(request.InputStream))
                            {
                                foreach (var zipEntry in packageReader.Entries)
                                {
                                    if (!zipEntry.IsDirectory)
                                    {
                                        ProcessMediaUpload(zipEntry.GetStream(), scriptDb, itemParam, zipEntry.Name, skipExisting);
                                    }
                                }
                            }
                        }
                        else
                        {
                            ProcessMediaUpload(request.InputStream, scriptDb, itemParam, null, skipExisting);
                        }
                        break;

                    case "file":
                        ProcessFileUpload(request.InputStream, originParam, pathParam);
                        break;
                    default:
                        PowerShellLog.Error($"Requested API/Version ({apiVersion}) is not supported.");
                        break;
                }
                return;
            }

            Item scriptItem;

            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(itemParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + itemParam);
                    break;
                case "media":
                    ProcessMediaDownload(scriptDb, itemParam);
                    return;
                case "file":
                    ProcessFileDownload(originParam, pathParam);
                    return;
                case "handle":
                    ProcessHandle(originParam);
                    return;
                default:
                    UpdateCache(dbName);
                    if (!apiScripts.ContainsKey(dbName))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    var dbScripts = apiScripts[scriptDb.Name];
                    if (!dbScripts.ContainsKey(itemParam))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    scriptItem = scriptDb.GetItem(dbScripts[itemParam].Id);
                    apiScripts = null;
                    break;
            }

            ProcessScript(context, scriptItem);
        }

        public bool IsReusable => true;

        public void InvalidateCache(object sender, EventArgs e)
        {
            apiScripts = null;
        }

        private static bool CheckServiceEnabled(string apiVersion, string httpMethod)
        {
            var isEnabled = true;
            const string disabledMessage = "The request could not be completed because the service is disabled.";

            switch (apiVersion)
            {
                case "1":
                    if (!WebServiceSettings.ServiceEnabledRestfulv1)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        HttpContext.Current.Response.StatusDescription = disabledMessage;
                        isEnabled = false;
                    }
                    break;
                case "2":
                    if (!WebServiceSettings.ServiceEnabledRestfulv2)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        HttpContext.Current.Response.StatusDescription = disabledMessage;
                        isEnabled = false;
                    }
                    break;
                case "file":
                    if ((WebServiceSettings.ServiceEnabledFileUpload && httpMethod.Is("POST")) ||
                        (WebServiceSettings.ServiceEnabledFileDownload && httpMethod.Is("GET")))
                    {
                        break;
                    }
                    HttpContext.Current.Response.StatusCode = 403;
                    HttpContext.Current.Response.StatusDescription = disabledMessage;
                    isEnabled = false;
                    break;
                case "media":
                    if ((WebServiceSettings.ServiceEnabledMediaUpload && httpMethod.Is("POST")) ||
                        (WebServiceSettings.ServiceEnabledMediaDownload && httpMethod.Is("GET")))
                    {
                        break;
                    }
                    HttpContext.Current.Response.StatusCode = 403;
                    HttpContext.Current.Response.StatusDescription = disabledMessage;
                    isEnabled = false;
                    break;
                case "handle":
                    if (!WebServiceSettings.ServiceEnabledHandleDownload)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        HttpContext.Current.Response.StatusDescription = disabledMessage;
                        isEnabled = false;
                    }
                    break;
                default:
                    HttpContext.Current.Response.StatusCode = 403;
                    HttpContext.Current.Response.StatusDescription = disabledMessage;
                    isEnabled = false;
                    break;
            }

            return isEnabled;
        }

        private static bool CheckServiceAuthentication(string apiVersion, bool isAuthenticated)
        {
            var skipAuthentication = false;
            const string disabledMessage = "The request could not be completed because the service requires authentication.";

            switch (apiVersion)
            {
                case "1":
                case "2":
                    skipAuthentication = true;
                    break;
                default:
                    if (!isAuthenticated)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        HttpContext.Current.Response.StatusDescription = disabledMessage;
                    }
                    break;
            }

            return skipAuthentication || isAuthenticated;
        }

        private static string GetPathFromParameters(string originParam, string pathParam)
        {
            var folder = string.Empty;

            switch (originParam)
            {
                case "data":
                    folder = Settings.DataFolder;
                    break;
                case "log":
                    folder = Settings.LogFolder;
                    break;
                case "media":
                    folder = Settings.MediaFolder;
                    break;
                case "package":
                    folder = Settings.PackagePath;
                    break;
                case "serialization":
                    folder = Settings.SerializationFolder;
                    break;
                case "temp":
                    folder = Settings.TempFolderPath;
                    break;
                case "debug":
                    folder = Settings.DebugFolder;
                    break;
                case "layout":
                    folder = Settings.LayoutFolder;
                    break;
                case "app":
                    folder = HttpRuntime.AppDomainAppPath;
                    break;
                default:
                    if (Path.IsPathRooted(pathParam))
                    {
                        folder = pathParam;
                    }
                    break;
            }

            if (folder != pathParam && !string.IsNullOrEmpty(pathParam))
            {
                folder = Path.GetFullPath(StringUtil.EnsurePostfix('\\', folder) + pathParam);
            }

            return folder;
        }

        private static void ProcessMediaUpload(Stream content, Database db, string itemParam, string entryName, bool skipExisting = false)
        {
            var mediaItem = (MediaItem)db.GetItem(itemParam) ?? db.GetItem(itemParam.TrimStart('/', '\\')) ??
                            db.GetItem(ApplicationSettings.MediaLibraryPath + itemParam);

            if (mediaItem == null)
            {
                var filename = itemParam.TrimEnd('/', '\\').Replace('\\', '/');
                var dirName = (Path.GetDirectoryName(filename) ?? string.Empty).Replace('\\', '/');
                if (!dirName.StartsWith(Constants.MediaLibraryPath))
                {
                    dirName = Constants.MediaLibraryPath + (dirName.StartsWith("/") ? dirName : "/" + dirName);
                }

                if (!String.IsNullOrEmpty(entryName))
                {
                    dirName += "/" + Path.GetDirectoryName(entryName).Replace('\\', '/');
                    filename = Path.GetFileName(entryName);
                }

                var mco = new MediaCreatorOptions
                {
                    Database = db,
                    Versioned = Settings.Media.UploadAsVersionableByDefault,
                    Destination = $"{dirName}/{Path.GetFileNameWithoutExtension(filename)}",
                };

                var mc = new MediaCreator();
                using (var ms = new MemoryStream())
                {
                    content.CopyTo(ms);
                    mc.CreateFromStream(ms, Path.GetFileName(filename), mco);
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
                    using (new EditContext(mediaItem, SecurityCheck.Disable))
                    {
                        using (var mediaStream = new MediaStream(ms, media.Extension, mediaItem))
                        {
                            media.SetStream(mediaStream);
                        }
                    }
                }
            }
        }

        private static void ProcessFileUpload(Stream content, string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam.Replace('/', '\\'));
            file = FileUtil.MapPath(file);
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
            {
                fileInfo.Directory?.Create();
            }
            using (var output = fileInfo.OpenWrite())
            {
                using (var input = content)
                {
                    input.CopyTo(output);
                }
            }
        }

        private static void ProcessMediaDownload(Database db, string itemParam)
        {
            itemParam = itemParam.TrimEnd('/', '\\').Replace('\\', '/');
            var mediaItem = (MediaItem)db.GetItem(itemParam) ?? db.GetItem(itemParam.TrimStart('/', '\\')) ??
                            db.GetItem(ApplicationSettings.MediaLibraryPath + itemParam);
            if (mediaItem == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                return;
            }

            var mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                return;
            }

            var str = mediaItem.Extension;
            if (!str.StartsWith(".", StringComparison.InvariantCulture))
                str = "." + str;
            WriteCacheHeaders(mediaItem.Name + str, mediaItem.Size);
            WebUtil.TransmitStream(mediaStream, HttpContext.Current.Response, Settings.Media.StreamBufferSize);
        }

        private static void ProcessFileDownload(string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam);

            if (string.IsNullOrEmpty(file))
            {
                HttpContext.Current.Response.StatusCode = 404;
            }
            else
            {
                file = FileUtil.MapPath(file);
                if (!File.Exists(file))
                {
                    HttpContext.Current.Response.StatusCode = 404;
                    return;
                }

                var fileInfo = new FileInfo(file);
                WriteCacheHeaders(fileInfo.Name, fileInfo.Length);
                HttpContext.Current.Response.TransmitFile(file);
            }
        }

        private static void ProcessScript(HttpContext context, Item scriptItem)
        {
            if (scriptItem?.Fields[ScriptItemFieldNames.Script] == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                return;
            }

            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                var script = scriptItem.Fields[ScriptItemFieldNames.Script].Value;

                if (Context.Database != null)
                {
                    var item = Context.Database.GetRootItem();
                    if (item != null)
                        session.SetItemLocationContext(item);
                }
                session.SetExecutedScript(scriptItem);

                context.Response.ContentType = "text/plain";

                var scriptArguments = new Hashtable();

                foreach (var param in HttpContext.Current.Request.QueryString.AllKeys)
                {
                    var paramValue = HttpContext.Current.Request.QueryString[param];
                    if (string.IsNullOrEmpty(param)) continue;
                    if (string.IsNullOrEmpty(paramValue)) continue;

                    scriptArguments[param] = paramValue;
                }

                foreach (var param in HttpContext.Current.Request.Params.AllKeys)
                {
                    var paramValue = HttpContext.Current.Request.Params[param];
                    if (string.IsNullOrEmpty(param)) continue;
                    if (string.IsNullOrEmpty(paramValue)) continue;

                    if (session.GetVariable(param) == null)
                    {
                        session.SetVariable(param, paramValue);
                    }
                }

                session.SetVariable("scriptArguments", scriptArguments);

                session.ExecuteScriptPart(script, true);

                context.Response.Write(session.Output.ToString());

                if (session.Output.HasErrors)
                {
                    context.Response.StatusCode = 424;
                    context.Response.StatusDescription = "Method Failure";
                }
            }
        }

        private static void ProcessHandle(string originParam)
        {
            if (string.IsNullOrEmpty(originParam))
            {
                HttpContext.Current.Response.StatusCode = 404;
            }
            else
            {
                // download handle
                var values = WebUtil.GetSessionValue(originParam) as OutDownloadMessage;
                if (values == null)
                {
                    HttpContext.Current.Response.StatusCode = 404;
                    return;
                }
                WebUtil.RemoveSessionValue(originParam);
                var Response = HttpContext.Current.Response;
                Response.Clear();
                Response.AddHeader("Content-Disposition", "attachment; filename=" + values.Name);
                Response.ContentType = values.ContentType;
                var fileContent = values.Content as FileInfo;
                if (fileContent != null)
                {
                    Response.WriteFile(fileContent.FullName);
                }

                var strContent = values.Content as string;
                if (strContent != null)
                {
                    Response.Output.Write("{0}", strContent);
                    Response.AddHeader("Content-Length",
                        strContent.Length.ToString(CultureInfo.InvariantCulture));
                }

                var byteContent = values.Content as byte[];
                if (byteContent != null)
                {
                    Response.OutputStream.Write(byteContent, 0, byteContent.Length);
                    Response.AddHeader("Content-Length",
                    byteContent.Length.ToString(CultureInfo.InvariantCulture));
                }

                Response.End();
            }
        }

        private void UpdateCache(string dbName)
        {
            if (apiScripts == null)
            {
                apiScripts = new SortedDictionary<string, SortedDictionary<string, ApiScript>>(StringComparer.OrdinalIgnoreCase);
            }

            if (ApplicationSettings.ScriptLibraryDb.Equals(dbName, StringComparison.OrdinalIgnoreCase))
            {
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi);
                BuildCache(roots);
                return;
            }

            if (!apiScripts.ContainsKey(dbName))
            {
                apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi, dbName);
                BuildCache(roots);
            }
        }

        private void BuildCache(IEnumerable<Item> roots)
        {
            foreach (var root in roots)
            {
                var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                var rootPath = root.Paths.Path;
                var query = $"{path}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\"]";
                try
                {
                    var results = root.Database.SelectItems(query);
                    foreach (var result in results)
                    {
                        var scriptPath = result.Paths.Path.Substring(rootPath.Length);
                        var dbName = result.Database.Name;
                        if (!apiScripts.ContainsKey(dbName))
                        {
                            apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                        }
                        apiScripts[dbName].Add(scriptPath, new ApiScript
                        {
                            Database = result.Database.Name,
                            Id = result.ID,
                            Path = scriptPath
                        });
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error("Error while querying for items", ex);
                }
            }
        }

        private static void WriteCacheHeaders(string filename, long length)
        {
            Assert.ArgumentNotNull(filename, "filename");
            var response = HttpContext.Current.Response;
            response.ClearHeaders();
            response.AddHeader("Content-Type", MimeMapping.GetMimeMapping(filename));
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
            response.AddHeader("Content-Length", length.ToString());
            response.AddHeader("Content-Transfer-Encoding", "binary");
        }

        public class ApiScript
        {
            public string Path { get; set; }
            public string Database { get; set; }
            public ID Id { get; set; }
        }
    }
}