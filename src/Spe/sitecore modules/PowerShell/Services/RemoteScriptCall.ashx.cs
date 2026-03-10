using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Sites;
using Sitecore.StringExtensions;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Interactive.Messages;
using Spe.Commands.Security;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.Utility;
using Spe.Core.VersionDecoupling;

namespace Spe.sitecore_modules.PowerShell.Services
{
    /// <summary>
    ///     The handler allows for execution of scripts stored within Script Library
    ///     it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler, IRequiresSessionState
    {
        private const string ApiScriptsKey = "Spe.ApiScriptsKey";
        private const string ExpirationSetting = "Spe.WebApiCacheExpirationSecs";

        private const string ParamUser = "user";
        private const string ParamPassword = "password";
        private const string ParamApiVersion = "apiVersion";
        private const string ParamScript = "script";
        private const string ParamPath = "path";
        private const string ParamScriptDb = "scriptDb";
        private const string ParamSessionId = "sessionId";
        private const string ParamPersistentSession = "persistentSession";
        private const string ParamRawOutput = "rawOutput";
        private const string ParamSkipUnpack = "skipunpack";
        private const string ParamSkipExisting = "skipexisting";
        private const string ParamScDatabase = "sc_database";
        private static readonly Regex GuidRegex = new Regex(@"(?<id>{[a-z0-9]{8}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{4}[-][a-z0-9]{12}})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly object ApiScriptsLock = new object();
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
            var request = context.Request;
            var apiVersion = request.Params.Get(ParamApiVersion);
            var serviceMappingKey = request.HttpMethod + "/" + apiVersion;
            if (!ApiVersionToServiceMapping.TryGetValue(serviceMappingKey, out var serviceName))
            {
                serviceName = string.Empty;
            }

            PowerShellLog.Info($"A request to the {serviceName} service was made from IP {GetIp(request)}");
            PowerShellLog.Debug($"'{request.Url}'");

            if (!CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }

            DispatchRequest(context, request, apiVersion, serviceMappingKey, identity, isAuthenticated);
        }

        private static bool AuthenticateRequest(HttpContext context, string serviceName, out AccountIdentity identity, out bool isAuthenticated)
        {
            var request = context.Request;
            var requestParameters = request.Params;
            var authenticationManager = TypeResolver.ResolveFromCache<IAuthenticationManager>();
            var username = requestParameters.Get(ParamUser);
            var password = requestParameters.Get(ParamPassword);
            var authHeader = request.Headers["Authorization"];

            identity = null;
            isAuthenticated = false;

            if (!string.IsNullOrEmpty(request.QueryString[ParamUser]) || !string.IsNullOrEmpty(request.QueryString[ParamPassword]))
            {
                PowerShellLog.Warn($"Credentials passed via query string from IP {GetIp(request)}. Query string authentication is deprecated — use the Authorization header instead.");
            }

            if (string.IsNullOrEmpty(username) && string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(authHeader))
            {
                if (authHeader.StartsWith("Basic"))
                {
                    var encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                    var encoding = Encoding.GetEncoding("iso-8859-1");
                    var usernamePassword = encoding.GetString(System.Convert.FromBase64String(encodedUsernamePassword));
                    var separatorIndex = usernamePassword.IndexOf(':');
                    username = usernamePassword.Substring(0, separatorIndex);
                    password = usernamePassword.Substring(separatorIndex + 1);
                }

                if (authHeader.StartsWith("Bearer"))
                {
                    var token = authHeader.Substring("Bearer ".Length).Trim();
                    try
                    {
                        if (ServiceAuthenticationManager.AuthenticationProvider.Validate(token, request.Url.GetLeftPart(UriPartial.Authority), out username))
                        {
                            authenticationManager.SwitchToUser(username, true);
                        }
                        else
                        {
                            RejectAuthenticationMethod(context, serviceName);
                            return false;
                        }
                    }
                    catch (SecurityException ex)
                    {
                        RejectAuthenticationMethod(context, serviceName, ex);
                        return false;
                    }
                }
            }

            var authUserName = string.IsNullOrEmpty(username) ? authenticationManager.CurrentUsername : username;

            if (string.IsNullOrEmpty(authUserName))
            {
                RejectAuthenticationMethod(context, serviceName);
                return false;
            }

            identity = new AccountIdentity(authUserName);
            if (!string.IsNullOrEmpty(password))
            {
                try
                {
                    if (authenticationManager.ValidateUser(identity.Name, password))
                    {
                        authenticationManager.SwitchToUser(identity.Name, true);
                    }
                    else
                    {
                        RejectAuthenticationMethod(context, serviceName);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    RejectAuthenticationMethod(context, serviceName, ex);
                    return false;
                }
            }

            if (!CheckIsUserAuthorized(context, identity.Name, serviceName))
            {
                return false;
            }

            isAuthenticated = authenticationManager.IsAuthenticated;

            if (identity.Name != authenticationManager.CurrentUsername && !CheckServiceAuthentication(context, serviceName, isAuthenticated))
            {
                return false;
            }

            return true;
        }

        private static void DispatchRequest(HttpContext context, HttpRequest request, string apiVersion, string serviceMappingKey, AccountIdentity identity, bool isAuthenticated)
        {
            var requestParameters = request.Params;
            var itemParam = requestParameters.Get(ParamScript);
            var pathParam = requestParameters.Get(ParamPath);
            var originParam = requestParameters.Get(ParamScriptDb);
            var sessionId = requestParameters.Get(ParamSessionId);
            var persistentSession = requestParameters.Get(ParamPersistentSession).Is("true");
            var rawOutput = requestParameters.Get(ParamRawOutput).Is("true");
            var isUpload = request.HttpMethod.Is("POST") && request.InputStream.Length > 0;
            var unpackZip = requestParameters.Get(ParamSkipUnpack).IsNot("true");
            var skipExisting = requestParameters.Get(ParamSkipExisting).Is("true");
            var scDb = requestParameters.Get(ParamScDatabase);

            var useContextDatabase = apiVersion.Is("file") || apiVersion.Is("handle") || !isAuthenticated ||
                                     string.IsNullOrEmpty(originParam) || originParam.Is("current");

            if (!scDb.IsNullOrEmpty())
            {
                Context.Database = Database.GetDatabase(scDb);
            }

            var scriptDb = useContextDatabase ? Context.Database : Database.GetDatabase(originParam);
            var dbName = scriptDb?.Name;

            if (scriptDb == null && !apiVersion.Is("file") && !apiVersion.Is("handle"))
            {
                PowerShellLog.Error($"The '{serviceMappingKey}' service requires a database but none was found in parameters or Context.");
                return;
            }

            Item scriptItem = null;

            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(itemParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + itemParam);
                    break;
                case "media":
                    ProcessMedia(context, isUpload, scriptDb, itemParam, unpackZip, skipExisting);
                    return;
                case "file":
                    ProcessFile(context, isUpload, originParam, pathParam);
                    return;
                case "handle":
                    ProcessHandle(context, originParam);
                    return;
                case "2":
                    var apiScripts = GetApiScripts(dbName);
                    if (apiScripts.TryGetValue(dbName, out var dbScripts) &&
                        dbScripts.TryGetValue(itemParam, out var apiScript))
                    {
                        scriptItem = scriptDb.GetItem(apiScript.Id);
                    }

                    if (scriptItem == null)
                    {
                        SetErrorResponse(context, 404, "The specified script is invalid.");
                        return;
                    }
                    break;
                case "script":
                    ProcessScript(context, request, rawOutput, sessionId, persistentSession);
                    return;
                default:
                    PowerShellLog.Error($"Requested API/Version ({serviceMappingKey}) is not supported.");
                    return;
            }

            ProcessScript(context, scriptItem);
        }

        private static string GetIp(HttpRequest request)
        {
            var ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrEmpty(ip))
            {
                ip = request.ServerVariables["REMOTE_ADDR"];
            }

            return ip;
        }

        public bool IsReusable => true;

        private static void SetErrorResponse(HttpContext context, int statusCode, string message, bool suppressFormsAuth = false)
        {
            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = message;
            if (suppressFormsAuth)
            {
                context.Response.SuppressFormsAuthenticationRedirect = true;
                context.Response.TrySkipIisCustomErrors = true;
                context.Response.ContentType = "text/plain";
            }
        }

        private static bool CheckServiceEnabled(HttpContext context, string serviceName)
        {
            var isEnabled = WebServiceSettings.IsEnabled(serviceName);
            if (isEnabled) return true;

            var errorMessage = $"The request could not be completed because the {serviceName} service is disabled.";
            SetErrorResponse(context, 403, errorMessage);
            PowerShellLog.Warn(errorMessage);

            return false;
        }

        private static bool CheckServiceAuthentication(HttpContext context, string serviceName, bool isAuthenticated)
        {
            if (isAuthenticated) return true;

            var errorMessage =
                $"The request could not be completed because the {serviceName} service requires authentication. Either the user is not logged in, authentication failed, or no credentials provided.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn(errorMessage);

            return false;
        }

        private static void RejectAuthenticationMethod(HttpContext context, string serviceName, Exception ex = null)
        {
            var errorMessage = $"A request to the {serviceName} service could not be completed because the provided credentials are invalid.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn(errorMessage);

            if (ex != null)
            {
                context.Response.StatusDescription += $" {ex.Message}";
                PowerShellLog.Error(ex.Message);
            }
        }

        private static bool CheckIsUserAuthorized(HttpContext context, string authUserName, string serviceName)
        {
            var isAuthorized = ServiceAuthorizationManager.IsUserAuthorized(serviceName, authUserName);
            if (isAuthorized) return true;

            var errorMessage = $"The specified user {authUserName} is not authorized for the {serviceName} service.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn(errorMessage);

            return false;
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
                folder = FileUtil.MapPath(StringUtil.EnsurePostfix('\\', folder) + pathParam);
            }

            return folder;
        }

        private static void ProcessFile(HttpContext context, bool isUpload, string originParam, string pathParam)
        {
            if (!string.IsNullOrEmpty(pathParam) && pathParam.Contains(".."))
            {
                PowerShellLog.Error($"Rejected file path with traversal attempt: '{pathParam}'");
                SetErrorResponse(context, 403, "Path traversal is not allowed.");
                return;
            }

            if (isUpload)
            {
                ProcessFileUpload(context.Request.InputStream, originParam, pathParam);
            }
            else
            {
                ProcessFileDownload(context, originParam, pathParam);
            }
        }

        private static void ProcessFileUpload(Stream content, string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam.Replace('/', '\\'));
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

        private static void ProcessFileDownload(HttpContext context, string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam);

            if (string.IsNullOrEmpty(file))
            {
                SetErrorResponse(context, 404, "The specified path is invalid.");
            }
            else
            {
                if (!File.Exists(file))
                {
                    SetErrorResponse(context, 404, "The specified path is invalid.");
                    return;
                }

                var fileInfo = new FileInfo(file);
                AddContentHeaders(context, fileInfo.Name, fileInfo.Length);
                try
                {
                    context.Response.TransmitFile(file);
                }
                catch (IOException _)
                {
                    context.Response.StatusCode = 500;
                    context.Response.StatusDescription = _.Message;
                }
            }
        }

        private static void ProcessMedia(HttpContext context, bool isUpload, Database scriptDb, string itemParam, bool unpackZip, bool skipExisting)
        {
            var request = context.Request;
            if (isUpload)
            {
                if (ZipUtils.IsZipContent(request.InputStream) && unpackZip)
                {
                    PowerShellLog.Debug("The uploaded asset will be extracted to Media Library.");
                    using (var packageReader = new ZipArchive(request.InputStream))
                    {
                        itemParam = Path.GetDirectoryName(itemParam.TrimEnd('\\', '/'));
                        foreach (var zipEntry in packageReader.Entries)
                        {
                            // ZipEntry does not provide an IsDirectory or IsFile property.
                            if (!(zipEntry.FullName.EndsWith("/") && zipEntry.Name == "") && zipEntry.Length > 0)
                            {
                                ProcessMediaUpload(zipEntry.Open(), scriptDb, $"{itemParam}/{zipEntry.FullName}",
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
                ProcessMediaDownload(context, scriptDb, itemParam);
            }
        }

        private static void ProcessMediaUpload(Stream content, Database db, string path, bool skipExisting = false)
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

        private static void ProcessMediaDownload(HttpContext context, Database db, string itemParam)
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
                SetErrorResponse(context, 404, "The specified media is invalid.");
                return;
            }

            var mediaStream = mediaItem.GetMediaStream();
            if (mediaStream == null)
            {
                SetErrorResponse(context, 404, "The specified media is invalid.");
                return;
            }

            var str = mediaItem.Extension;
            if (!str.StartsWith(".", StringComparison.InvariantCulture))
                str = "." + str;
            AddContentHeaders(context, mediaItem.Name + str, mediaItem.Size);
            WebUtil.TransmitStream(mediaStream, context.Response, Settings.Media.StreamBufferSize);
        }

        private static byte[] ConvertFromGzipBytes(byte[] gzip)
        {
            using (var stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }

        private static void ProcessScript(HttpContext context, HttpRequest request, bool rawOutput, string sessionId, bool persistentSession)
        {
            if (request?.InputStream == null) return;

            string script = null;
            string cliXmlArgs = null;
            using (var ms = new MemoryStream())
            {
                request.InputStream.CopyTo(ms);
                var shouldDecompress = request.Headers["Content-Encoding"]?.Contains("gzip") ?? false ;
                var bytes = shouldDecompress ? ConvertFromGzipBytes(ms.ToArray()) : ms.ToArray();
                var requestBody = Encoding.UTF8.GetString(bytes);
                var splitBody = requestBody.Split(new[] { $"<#{sessionId}#>" }, StringSplitOptions.RemoveEmptyEntries);
                if (splitBody.Length > 0)
                {
                    script = splitBody[0];
                    if (splitBody.Length > 1)
                    {
                        cliXmlArgs = splitBody[1];
                    }
                }
            }

            ProcessScript(context, script, null, cliXmlArgs, rawOutput, sessionId, persistentSession);
        }

        private static void ProcessScript(HttpContext context, Item scriptItem)
        {
            if (!scriptItem.IsPowerShellScript() || scriptItem?.Fields[Templates.Script.Fields.ScriptBody] == null)
            {
                SetErrorResponse(context, 404, "The specified script is invalid.");
                return;
            }

            var script = scriptItem[Templates.Script.Fields.ScriptBody];

            var streams = new Dictionary<string, Stream>();
            var request = context.Request;
            if (request.Files.AllKeys.Length > 0)
            {
                foreach (var fileName in request.Files.AllKeys)
                {
                    streams.Add(fileName, request.Files[fileName]?.InputStream);
                }
            }
            else
            {
                streams.Add("stream", request.InputStream);
            }

            ProcessScript(context, script, streams);
        }

        private static void ProcessScript(HttpContext context, string script, Dictionary<string, Stream> streams, string cliXmlArgs = null, bool rawOutput = false, string sessionId = null, bool persistentSession = false)
        {
            if (string.IsNullOrEmpty(script))
            {
                SetErrorResponse(context, 400, "The specified script is invalid.");
                return;
            }

            var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            try
            {
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
                    foreach (var param in context.Request.QueryString.AllKeys)
                    {
                        var paramValue = HttpContext.Current.Request.QueryString[param];
                        if (string.IsNullOrEmpty(param)) continue;
                        if (string.IsNullOrEmpty(paramValue)) continue;

                        scriptArguments[param] = paramValue;
                    }

                    foreach (var param in context.Request.Params.AllKeys)
                    {
                        if (string.IsNullOrEmpty(param)) continue;
                        if (param.StartsWith("ALL_") || param.StartsWith("HTTP_") ||
                            param.StartsWith("SERVER_") || param.StartsWith("REMOTE_") ||
                            param.StartsWith("LOCAL_") || param.StartsWith("CERT_") ||
                            param.StartsWith("HTTPS") || param.StartsWith("APP_") ||
                            param.StartsWith("AUTH_") || param.StartsWith("CONTENT_") ||
                            param.StartsWith("APPL_") || param.StartsWith("INSTANCE_") ||
                            param.StartsWith("GATEWAY_") || param.StartsWith("PATH_") ||
                            param.StartsWith("SCRIPT_") || param.StartsWith("URL") ||
                            param.StartsWith("CACHE_") || param.StartsWith("LOGON_") ||
                            param.StartsWith("REQUEST_") || param.StartsWith("UNENCODED_") ||
                            param.StartsWith("UNMAPPED_"))
                            continue;

                        var paramValue = context.Request.Params[param];
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
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("Error during script execution via RemoteScriptCall", ex);
                context.Response.StatusCode = 424;
                context.Response.StatusDescription = "Method Failure";
            }
            finally
            {
                if (string.IsNullOrEmpty(sessionId) || !persistentSession)
                {
                    ScriptSessionManager.RemoveSession(session);
                }
            }
        }

        private static void ProcessHandle(HttpContext context, string originParam)
        {
            if (originParam.IsNullOrEmpty())
            {
                context.Response.StatusCode = 404;
            }
            else
            {
                // download handle
                if (!(WebUtil.GetSessionValue(originParam) is OutDownloadMessage message))
                {
                    context.Response.StatusCode = 404;
                    return;
                }
                WebUtil.RemoveSessionValue(originParam);
                var response = context.Response;
                response.Clear();
                response.AddHeader("Content-Disposition", "attachment; filename=\"" + SanitizeContentDispositionFilename(message.Name) + "\"");
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

        private static ApiScriptCollection GetApiScripts(string dbName)
        {
            Assert.ArgumentNotNullOrEmpty(dbName, "dbName");
            if (HttpRuntime.Cache[ApiScriptsKey] is ApiScriptCollection cachedScripts) return cachedScripts;

            lock (ApiScriptsLock)
            {
                if (HttpRuntime.Cache[ApiScriptsKey] is ApiScriptCollection doubleCheckScripts) return doubleCheckScripts;

                var apiScripts = new ApiScriptCollection();

                if (ApplicationSettings.ScriptLibraryDb.Equals(dbName, StringComparison.OrdinalIgnoreCase))
                {
                    var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi);
                    GetAvailableScripts(roots, apiScripts);
                }
                else
                {
                    apiScripts.GetOrAdd(dbName, _ => new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                    var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi, dbName);
                    GetAvailableScripts(roots, apiScripts);
                }

                var expiration = Settings.GetIntSetting(ExpirationSetting, 30);
                HttpRuntime.Cache.Add(ApiScriptsKey, apiScripts, null, Cache.NoAbsoluteExpiration,
                    new TimeSpan(0, 0, expiration), CacheItemPriority.Normal, null);

                return apiScripts;
            }
        }

        private static void GetAvailableScripts(IEnumerable<Item> roots, ApiScriptCollection apiScripts)
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
                        var scripts = apiScripts.GetOrAdd(dbName, _ => new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                        scripts[scriptPath] = new ApiScript
                        {
                            Database = result.Database.Name,
                            Id = result.ID,
                            Path = scriptPath
                        };
                    }
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error("Error while querying for items", ex);
                }
            }
        }

        private static string SanitizeContentDispositionFilename(string filename)
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

        private static void AddContentHeaders(HttpContext context, string filename, long length)
        {
            Assert.ArgumentNotNull(filename, "filename");
            var response = context.Response;
            response.ClearHeaders();
            response.AddHeader("Content-Type", MimeMapping.GetMimeMapping(filename));
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + SanitizeContentDispositionFilename(filename) + "\"");
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

    internal class ApiScriptCollection : ConcurrentDictionary<string, SortedDictionary<string, ApiScript>> {}
}