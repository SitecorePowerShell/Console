using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.Web;
using Sitecore.StringExtensions;
using AuthenticationManager = Sitecore.Security.Authentication.AuthenticationManager;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    ///     The handler allows for execution of scripts stored within Script Library
    ///     it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler, IRequiresSessionState
    {
        private static readonly object LoginLock = new object();
        private SortedDictionary<string, SortedDictionary<string, ApiScript>> _apiScripts;
        private static readonly Dictionary<string, string> ApiVersionToServiceMapping = new Dictionary<string, string>()
        {
            { "POST/script" , WebServiceSettings.ServiceRemoting },
            { "GET/script" , WebServiceSettings.ServiceRemoting },
            { "POST/file" , WebServiceSettings.ServiceFileUpload },
            { "POST/media" , WebServiceSettings.ServiceMediaUpload },
            { "GET/1" , WebServiceSettings.ServiceRestfulv1 },
            { "POST/1" , WebServiceSettings.ServiceRestfulv1 },
            { "GET/2" , WebServiceSettings.ServiceRestfulv2 },
            { "POST/2" , WebServiceSettings.ServiceRestfulv2 },
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
            var sessionId = request.Params.Get("sessionId");
            var persistentSession = request.Params.Get("persistentSession").Is("true");
            var rawOutput = request.Params.Get("rawOutput").Is("true");
            var apiVersion = request.Params.Get("apiVersion");
            var serviceMappingKey = request.HttpMethod + "/" + apiVersion;
            var isUpload = request.HttpMethod.Is("POST") && request.InputStream.Length > 0;
            var unpackZip = request.Params.Get("skipunpack").IsNot("true");
            var skipExisting = request.Params.Get("skipexisting").Is("true");
            var scDb = request.Params.Get("sc_database");

            var serviceName = ApiVersionToServiceMapping.ContainsKey(serviceMappingKey)
                ? ApiVersionToServiceMapping[serviceMappingKey]
                : string.Empty;

            // verify that the service is enabled
            if (!CheckServiceEnabled(apiVersion, request.HttpMethod))
            {
                PowerShellLog.Error($"Attempt to call the {apiVersion} service failed as it is not enabled.");
                return;
            }

            // verify that the user is authorized to access the end point
            var authUserName = string.IsNullOrEmpty(userName) ? Context.User.Name : userName;
            var identity = new AccountIdentity(authUserName);

            if (!ServiceAuthorizationManager.IsUserAuthorized(serviceName, identity.Name))
            {
                HttpContext.Current.Response.StatusCode = 401;
                PowerShellLog.Error(
                    $"Attempt to call the '{serviceMappingKey}' service failed as user '{authUserName}' was not authorized.");
                return;
            }

            lock (LoginLock)
            {
                // login user if specified explicitly
                if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
                {
                    AuthenticationManager.Login(identity.Name, password, false);
                }
            }

            var isAuthenticated = Context.IsLoggedIn;

            // in some cases we need to set the database as it's still set to web after authentication
            if (!scDb.IsNullOrEmpty())
            {
                Context.Database = Database.GetDatabase(scDb);
            }

            var useContextDatabase = apiVersion.Is("file") || apiVersion.Is("handle") || !isAuthenticated ||
                                     string.IsNullOrEmpty(originParam) || originParam.Is("current");
            var scriptDb = useContextDatabase ? Context.Database : Database.GetDatabase(originParam);
            var dbName = scriptDb?.Name;

            if (!CheckServiceAuthentication(apiVersion, isAuthenticated))
            {
                PowerShellLog.Error(
                    $"Attempt to call the {serviceMappingKey} service failed as - user not logged in, authentication failed or no credentials provided.");
                return;
            }

            if (scriptDb == null && !apiVersion.Is("file") && !apiVersion.Is("handle"))
            {
                PowerShellLog.Error(
                    $"The '{serviceMappingKey}' service requires a database but none was found in parameters or Context.");
                return;
            }

            PowerShellLog.Info($"'{serviceMappingKey}' called by user: '{userName}'");
            PowerShellLog.Debug($"'{request.Url}'");

            Item scriptItem;

            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(itemParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + itemParam);
                    break;
                case "media":
                    ProcessMedia(request, isUpload, scriptDb, itemParam, unpackZip, skipExisting);
                    return;
                case "file":
                    ProcessFile(request, isUpload, originParam, pathParam);
                    return;
                case "handle":
                    ProcessHandle(originParam);
                    return;
                case "2":
                    UpdateCache(dbName);
                    if (!_apiScripts.ContainsKey(dbName))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        HttpContext.Current.Response.StatusDescription = "The specified script is invalid.";
                        return;
                    }
                    var dbScripts = _apiScripts[dbName];
                    if (!dbScripts.ContainsKey(itemParam))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        HttpContext.Current.Response.StatusDescription = "The specified script is invalid.";
                        return;
                    }
                    scriptItem = scriptDb.GetItem(dbScripts[itemParam].Id);
                    _apiScripts = null;
                    break;
                case "script":
                    ProcessScript(context, request, rawOutput, sessionId, persistentSession);
                    return;
                default:
                    PowerShellLog.Error($"Requested API/Version ({serviceMappingKey}) is not supported.");
                    return;
            }

            ProcessScript(context, request, scriptItem);
        }

        public bool IsReusable => true;

        private static bool CheckServiceEnabled(string apiVersion, string httpMethod)
        {
            var isEnabled = true;
            const string disabledMessage = "The request could not be completed because the service is disabled.";

            switch (apiVersion)
            {
                case "1":
                    isEnabled = WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRestfulv1);
                    break;
                case "2":
                    isEnabled = WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRestfulv2);
                    break;
                case "file":
                    isEnabled = (WebServiceSettings.IsEnabled(WebServiceSettings.ServiceFileUpload) &&
                                 httpMethod.Is("POST")) ||
                                (WebServiceSettings.IsEnabled(WebServiceSettings.ServiceFileDownload) &&
                                 httpMethod.Is("GET"));
                    break;
                case "media":
                    isEnabled = ((WebServiceSettings.IsEnabled(WebServiceSettings.ServiceMediaUpload) &&
                                  httpMethod.Is("POST")) ||
                                 (WebServiceSettings.IsEnabled(WebServiceSettings.ServiceMediaDownload) &&
                                  httpMethod.Is("GET")));
                    break;
                case "handle":
                    isEnabled = WebServiceSettings.IsEnabled(WebServiceSettings.ServiceHandleDownload);
                    break;
                case "script":
                    isEnabled = WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting);
                    break;
                default:
                    isEnabled = false;
                    break;
            }

            if (isEnabled) return true;

            HttpContext.Current.Response.StatusCode = 403;
            HttpContext.Current.Response.StatusDescription = disabledMessage;

            return false;
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
                case "index":
                    folder = Settings.IndexFolder;
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
                folder = FileUtil.MapPath(StringUtil.EnsurePostfix('\\', folder) + pathParam);
            }

            return folder;
        }

        private static void ProcessFile(HttpRequest request, bool isUpload, string originParam, string pathParam)
        {
            if (isUpload)
            {
                ProcessFileUpload(request.InputStream, originParam, pathParam);
            }
            else
            {
                ProcessFileDownload(originParam, pathParam);
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

        private static void ProcessFileDownload(string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam);

            if (string.IsNullOrEmpty(file))
            {
                HttpContext.Current.Response.StatusCode = 404;
                HttpContext.Current.Response.StatusDescription = "The specified path is invalid.";
            }
            else
            {
                file = FileUtil.MapPath(file);
                if (!File.Exists(file))
                {
                    HttpContext.Current.Response.StatusCode = 404;
                    HttpContext.Current.Response.StatusDescription = "The specified path is invalid.";
                    return;
                }

                var fileInfo = new FileInfo(file);
                WriteCacheHeaders(fileInfo.Name, fileInfo.Length);
                HttpContext.Current.Response.TransmitFile(file);
            }
        }

        private static void ProcessMedia(HttpRequest request, bool isUpload, Database scriptDb, string itemParam, bool unpackZip, bool skipExisting)
        {
            if (isUpload)
            {
                if (ZipUtils.IsZipContent(request.InputStream) && unpackZip)
                {
                    PowerShellLog.Debug("The uploaded asset will be extracted to Media Library.");
                    using (var packageReader = new Sitecore.Zip.ZipReader(request.InputStream))
                    {
                        itemParam = Path.GetDirectoryName(itemParam.TrimEnd('\\', '/'));
                        foreach (var zipEntry in packageReader.Entries)
                        {
                            if (!zipEntry.IsDirectory && zipEntry.Size > 0)
                            {
                                ProcessMediaUpload(zipEntry.GetStream(), scriptDb, $"{itemParam}/{zipEntry.Name}",
                                    skipExisting);
                            }
                        }
                    }
                }
                else if (request.Files?.AllKeys?.Length > 0)
                {
                    foreach (string fileName in request.Files.Keys)
                    {
                        var file = request.Files[fileName];
                        ProcessMediaUpload(file.InputStream, scriptDb, $"{itemParam}/{file.FileName}",
                            skipExisting);
                    }
                }
                else
                {
                    ProcessMediaUpload(request.InputStream, scriptDb, itemParam, skipExisting);
                }
            }
            else
            {
                ProcessMediaDownload(scriptDb, itemParam);
            }
        }

        private static void ProcessMediaUpload(Stream content, Database db, string path, bool skipExisting = false)
        {
            var guidPattern = @"(?<id>{[a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12}})";

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

            if (mediaItem == null && Regex.IsMatch(originalPath, guidPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase))
            {
                var id = Regex.Match(originalPath, guidPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase).Value;
                mediaItem = db.GetItem(id);
            }

            if (mediaItem == null)
            {
                var fileName = Path.GetFileName(originalPath);
                var itemName = Path.GetFileNameWithoutExtension(path);
                var dirName = (Path.GetDirectoryName(path) ?? string.Empty).Replace('\\', '/');

                if (String.IsNullOrEmpty(fileName))
                {
                    PowerShellLog.Warn($"The filename cannot be determined for the entry {fileName}.");
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
                    mc.CreateFromStream(ms, fileName, mco);
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

        private static void ProcessMediaDownload(Database db, string itemParam)
        {
            var indexOfDot = itemParam.IndexOf(".", StringComparison.Ordinal);
            itemParam = indexOfDot == -1 ? itemParam : itemParam.Substring(0, indexOfDot);
            itemParam = itemParam.Replace('\\', '/').TrimEnd('/');
            itemParam = itemParam.StartsWith("/") ? itemParam : $"/{itemParam}";
            itemParam = itemParam.StartsWith(ApplicationSettings.MediaLibraryPath, StringComparison.OrdinalIgnoreCase) ? itemParam : $"{ApplicationSettings.MediaLibraryPath}{itemParam}";

            var mediaItem = (MediaItem)db.GetItem(itemParam);
            if (mediaItem == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                HttpContext.Current.Response.StatusDescription = "The specified media is invalid.";
                return;
            }

            var mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                HttpContext.Current.Response.StatusDescription = "The specified media is invalid.";
                return;
            }

            var str = mediaItem.Extension;
            if (!str.StartsWith(".", StringComparison.InvariantCulture))
                str = "." + str;
            WriteCacheHeaders(mediaItem.Name + str, mediaItem.Size);
            WebUtil.TransmitStream(mediaStream, HttpContext.Current.Response, Settings.Media.StreamBufferSize);
        }

        private static void ProcessScript(HttpContext context, HttpRequest request, bool rawOutput, string sessionId, bool persistentSession)
        {
            if (request?.InputStream == null) return;

            string script;
            string cliXmlArgs = null;
            using (var ms = new MemoryStream())
            {
                request.InputStream.CopyTo(ms);
                var bytes = ms.ToArray();
                var requestBody = Encoding.UTF8.GetString(bytes);
                var splitBody = requestBody.Split(new[] { $"<#{sessionId}#>" }, StringSplitOptions.RemoveEmptyEntries);
                script = splitBody[0];
                if (splitBody.Length > 1)
                {
                    cliXmlArgs = splitBody[1];
                }
            }

            ProcessScript(context, script, null, cliXmlArgs, rawOutput, sessionId, persistentSession);
        }

        private static void ProcessScript(HttpContext context, HttpRequest request, Item scriptItem)
        {
            if (!scriptItem.IsPowerShellScript() || scriptItem?.Fields[Templates.Script.Fields.ScriptBody] == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                HttpContext.Current.Response.StatusDescription = "The specified script is invalid.";
                return;
            }

            var script = scriptItem[Templates.Script.Fields.ScriptBody];

            var streams = new Dictionary<string, Stream>();

            if (request.Files?.AllKeys?.Length > 0)
            {
                foreach (var fileName in request.Files.AllKeys)
                {
                    streams.Add(fileName, request.Files[fileName].InputStream);
                }
            }
            else if (request.InputStream != null)
            {
                streams.Add("stream", request.InputStream);
            }

            ProcessScript(context, script, streams);
        }

        private static void ProcessScript(HttpContext context, string script, Dictionary<string, Stream> streams, string cliXmlArgs = null, bool rawOutput = false, string sessionId = null, bool persistentSession = false)
        {
            if (string.IsNullOrEmpty(script))
            {
                HttpContext.Current.Response.StatusCode = 404;
                HttpContext.Current.Response.StatusDescription = "The specified script is invalid.";
                return;
            }

            var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            if (Context.Database != null)
            {
                var item = Context.Database.GetRootItem();
                if (item != null)
                    session.SetItemLocationContext(item);
            }

            context.Response.ContentType = "text/plain";

            if (streams != null)
            {
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

                session.SetVariable("requestStreams", streams);
                session.SetVariable("scriptArguments", scriptArguments);
                session.ExecuteScriptPart(script, true);
                context.Response.Write(session.Output.ToString());
            }
            else
            {
                // Duplicate the behaviors of the original RemoteAutomation service.
                var requestUri = WebUtil.GetRequestUri();
                var site = SiteContextFactory.GetSiteContext(requestUri.Host, Context.Request.FilePath,
                    requestUri.Port);
                Context.SetActiveSite(site.Name);

                if (!string.IsNullOrEmpty(cliXmlArgs))
                {
                    session.SetVariable("cliXmlArgs", cliXmlArgs);
                    session.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs", false, true);
                    script = script.TrimEnd(' ', '\t', '\n');
                }

                var outObjects = session.ExecuteScriptPart(script, false, false, false) ?? new List<object>();
                var response = context.Response;
                if (rawOutput)
                {
                    // In this output we want to give raw output data. No type information is needed. Error streams are lost.                       
                    if (outObjects.Any())
                    {
                        foreach (var outObject in outObjects)
                        {
                            response.Write(outObject.ToString());
                        }
                    }

                    if (session.LastErrors != null && session.LastErrors.Any())
                    {
                        var convertedObjects = new List<object>();
                        convertedObjects.AddRange(session.LastErrors);

                        session.SetVariable("results", convertedObjects);
                        session.Output.Clear();
                        session.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results");

                        response.Write("<#messages#>");
                        foreach (var outputBuffer in session.Output)
                        {
                            response.Write(outputBuffer.Text);
                        }
                    }
                }
                else
                {
                    // In this output we want to preserve type information. Ideal for objects with a small output content.
                    if (session.LastErrors != null && session.LastErrors.Any())
                    {
                        outObjects.AddRange(session.LastErrors);
                    }

                    if (outObjects.Any())
                    {
                        session.SetVariable("results", outObjects);
                        session.Output.Clear();
                        session.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results");

                        foreach (var outputBuffer in session.Output)
                        {
                            response.Write(outputBuffer.Text);
                        }
                    }
                }
            }

            if (session.Output.HasErrors)
            {
                context.Response.StatusCode = 424;
                context.Response.StatusDescription = "Method Failure";
            }

            if (string.IsNullOrEmpty(sessionId) || !persistentSession)
            {
                ScriptSessionManager.RemoveSession(session);
            }
        }

        private static void ProcessHandle(string originParam)
        {
            if (originParam.IsNullOrEmpty())
            {
                HttpContext.Current.Response.StatusCode = 404;
            }
            else
            {
                // download handle
                if (!(WebUtil.GetSessionValue(originParam) is OutDownloadMessage message))
                {
                    HttpContext.Current.Response.StatusCode = 404;
                    return;
                }
                WebUtil.RemoveSessionValue(originParam);
                var response = HttpContext.Current.Response;
                response.Clear();
                response.AddHeader("Content-Disposition", "attachment; filename=" + message.Name);
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

        private void UpdateCache(string dbName)
        {
            Assert.ArgumentNotNullOrEmpty(dbName, "dbName");
            if (_apiScripts == null)
            {
                _apiScripts = new SortedDictionary<string, SortedDictionary<string, ApiScript>>(StringComparer.OrdinalIgnoreCase);
            }

            if (ApplicationSettings.ScriptLibraryDb.Equals(dbName, StringComparison.OrdinalIgnoreCase))
            {
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi);
                BuildCache(roots);
                return;
            }

            if (!_apiScripts.ContainsKey(dbName))
            {
                _apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
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
                var query = $"{path}//*[@@TemplateId=\"{Templates.Script.Id}\"]";
                try
                {
                    var results = root.Database.SelectItems(query);
                    foreach (var result in results)
                    {
                        var scriptPath = result.Paths.Path.Substring(rootPath.Length);
                        var dbName = result.Database.Name;
                        if (!_apiScripts.ContainsKey(dbName))
                        {
                            _apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                        }
                        _apiScripts[dbName].Add(scriptPath, new ApiScript
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
    }

    internal class ApiScript
    {
        public string Path { get; set; }
        public string Database { get; set; }
        public ID Id { get; set; }
    }
}