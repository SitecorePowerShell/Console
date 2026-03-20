using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Management.Automation.Language;
using System.Security.Cryptography;
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
        private const string ParamOutputFormat = "outputFormat";
        private const string ParamErrorFormat = "errorFormat";

        private const string StructuredErrorScript =
            "@{ output = @($outObjects); errors = @($errorObjects | ForEach-Object { " +
            "$err = $_; $h = @{ " +
            "message = $err.ToString(); " +
            "errorCategory = \"$($err.CategoryInfo.Category)\"; " +
            "categoryReason = $err.CategoryInfo.Reason; " +
            "categoryTargetName = $err.CategoryInfo.TargetName; " +
            "categoryTargetType = $err.CategoryInfo.TargetType; " +
            "fullyQualifiedErrorId = $err.FullyQualifiedErrorId; " +
            "exceptionType = $(if ($err.Exception) { $err.Exception.GetType().FullName } else { $null }); " +
            "exceptionMessage = $(if ($err.Exception) { $err.Exception.Message } else { $null }); " +
            "scriptStackTrace = $err.ScriptStackTrace " +
            "}; " +
            "if ($err.InvocationInfo) { " +
            "$h['invocationInfo'] = @{ " +
            "scriptName = $err.InvocationInfo.ScriptName; " +
            "scriptLineNumber = $err.InvocationInfo.ScriptLineNumber; " +
            "offsetInLine = $err.InvocationInfo.OffsetInLine; " +
            "line = $err.InvocationInfo.Line; " +
            "positionMessage = $err.InvocationInfo.PositionMessage " +
            "} }; $h }) } | ConvertTo-Json -Depth 4 -Compress";

        private const string FlatErrorScript =
            "@{ output = @($outObjects); errors = @($errorObjects | ForEach-Object { $_.ToString() }) } | ConvertTo-Json -Depth 3 -Compress";
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
            var origin = request.Headers["Origin"];
            var apiVersion = request.Params.Get(ParamApiVersion);

            var serviceMappingKey = request.HttpMethod + "/" + apiVersion;
            if (!ApiVersionToServiceMapping.TryGetValue(serviceMappingKey, out var serviceName))
            {
                serviceName = string.Empty;
            }

            PowerShellLog.Info($"[{serviceName}] A request to the {serviceName} service was made from IP {GetIp(request)}");
            PowerShellLog.Debug($"'{request.Url}'");

            if (!CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }

            PowerShellLog.Info($"[{serviceName}] Authenticated request from IP {GetIp(request)}, user: {identity.Name}");

            DispatchRequest(context, request, apiVersion, serviceMappingKey, serviceName, identity, isAuthenticated);

            AddCorsHeaders(context, serviceName, origin);
        }

        private static void AddCorsHeaders(HttpContext context, string serviceName, string origin)
        {
            if (string.IsNullOrEmpty(origin))
            {
                return;
            }

            var cors = WebServiceSettings.GetCorsSettings(serviceName);
            if (cors == null)
            {
                return;
            }

            if (!IsOriginAllowed(cors, origin))
            {
                return;
            }

            var response = context.Response;
            response.Headers["Access-Control-Allow-Origin"] = cors.AllowAnyOrigin ? "*" : origin;

            if (cors.AllowCredentials)
            {
                response.Headers["Access-Control-Allow-Credentials"] = "true";
            }
        }

        private static bool IsOriginAllowed(WebServiceSettings.CorsSettings cors, string origin)
        {
            if (cors.AllowAnyOrigin)
            {
                return true;
            }

            return !string.IsNullOrEmpty(origin) && cors.AllowedOrigins != null && cors.AllowedOrigins.Contains(origin);
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
                PowerShellLog.Warn($"[{serviceName}] Credentials passed via query string from IP {GetIp(request)}, user: {username}. Query string authentication is deprecated — use the Authorization header instead.");
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
                    var ip = GetIp(request);
                    try
                    {
                        var provider = ServiceAuthenticationManager.AuthenticationProvider;
                        TokenValidationResult tokenResult = null;

                        bool isValid;
                        if (provider is ISpeAuthenticationProviderEx providerEx)
                        {
                            isValid = providerEx.Validate(token, request.Url.GetLeftPart(UriPartial.Authority), out username, out tokenResult);
                        }
                        else
                        {
                            isValid = provider.Validate(token, request.Url.GetLeftPart(UriPartial.Authority), out username);
                        }

                        if (isValid)
                        {
                            authenticationManager.SwitchToUser(username, true);
                            PowerShellLog.Audit("Remoting: Bearer auth success, user={0}, ip={1}, scope={2}, clientSession={3}",
                                username, ip, tokenResult?.Scope ?? "none", tokenResult?.ClientSessionId ?? "none");
                            if (tokenResult != null)
                            {
                                HttpContext.Current.Items["SpeTokenResult"] = tokenResult;
                            }
                        }
                        else
                        {
                            PowerShellLog.Audit("Remoting: Bearer auth failed, user={0}, ip={1}", username ?? "unknown", ip);
                            RejectAuthenticationMethod(context, serviceName, username);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Audit("Remoting: Bearer auth error, user={0}, ip={1}, error={2}", username ?? "unknown", ip, ex.Message);
                        RejectAuthenticationMethod(context, serviceName, username, ex);
                        return false;
                    }
                }
            }

            var authUserName = string.IsNullOrEmpty(username) ? authenticationManager.CurrentUsername : username;

            if (string.IsNullOrEmpty(authUserName))
            {
                RejectAuthenticationMethod(context, serviceName, username);
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
                        RejectAuthenticationMethod(context, serviceName, identity.Name);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    RejectAuthenticationMethod(context, serviceName, identity.Name, ex);
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

        private static void DispatchRequest(HttpContext context, HttpRequest request, string apiVersion, string serviceMappingKey, string serviceName, AccountIdentity identity, bool isAuthenticated)
        {
            var requestParameters = request.Params;
            var itemParam = requestParameters.Get(ParamScript);
            var pathParam = requestParameters.Get(ParamPath);
            var originParam = requestParameters.Get(ParamScriptDb);
            var sessionId = requestParameters.Get(ParamSessionId);
            var persistentSession = requestParameters.Get(ParamPersistentSession).Is("true");
            var rawOutput = requestParameters.Get(ParamRawOutput).Is("true");
            var outputFormat = requestParameters.Get(ParamOutputFormat) ?? string.Empty;
            if (rawOutput && string.IsNullOrEmpty(outputFormat))
                outputFormat = "raw";
            else if (string.IsNullOrEmpty(outputFormat))
                outputFormat = "clixml";
            var useStructuredErrors = requestParameters.Get(ParamErrorFormat).Is("structured");
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
                PowerShellLog.Error($"[{serviceMappingKey}] The '{serviceMappingKey}' service requires a database but none was found in parameters or Context.");
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
                    ProcessMedia(context, serviceName, isUpload, scriptDb, itemParam, unpackZip, skipExisting);
                    return;
                case "file":
                    ProcessFile(context, serviceName, isUpload, originParam, pathParam);
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
                    ProcessScript(context, request, serviceName, outputFormat, sessionId, persistentSession, useStructuredErrors);
                    return;
                default:
                    PowerShellLog.Error($"[{serviceMappingKey}] Requested API/Version ({serviceMappingKey}) is not supported.");
                    return;
            }

            ProcessScript(context, scriptItem, serviceName);
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

        private static string ComputeScriptHash(string script)
        {
            if (string.IsNullOrEmpty(script)) return "empty";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(script));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 16);
            }
        }

        public bool IsReusable => true;

        private static void SetErrorResponse(HttpContext context, int statusCode, string message, bool suppressFormsAuth = false)
        {
            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = message;

            var outputFormat = context.Request.QueryString[ParamOutputFormat];
            if (!string.IsNullOrEmpty(outputFormat) && outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/json";
                var useStructured = context.Request.QueryString[ParamErrorFormat].Is("structured");
                if (useStructured)
                {
                    var errorBody = new Hashtable
                    {
                        ["output"] = new object[0],
                        ["errors"] = new[]
                        {
                            new Hashtable
                            {
                                ["message"] = message,
                                ["errorCategory"] = statusCode == 401 || statusCode == 403 ? "SecurityError" : "ConnectionError",
                                ["exceptionType"] = "System.Web.HttpException",
                                ["exceptionMessage"] = message
                            }
                        }
                    };
                    context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(errorBody));
                }
                else
                {
                    var errorBody = new Hashtable
                    {
                        ["output"] = new object[0],
                        ["errors"] = new[] { message }
                    };
                    context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(errorBody));
                }
            }

            if (suppressFormsAuth)
            {
                context.Response.SuppressFormsAuthenticationRedirect = true;
                context.Response.TrySkipIisCustomErrors = true;
                if (context.Response.ContentType != "application/json")
                {
                    context.Response.ContentType = "text/plain";
                }
            }
        }

        private static bool CheckServiceEnabled(HttpContext context, string serviceName)
        {
            var isEnabled = WebServiceSettings.IsEnabled(serviceName);
            if (isEnabled) return true;

            var ip = GetIp(context.Request);
            var errorMessage = $"[{serviceName}] The request could not be completed because the {serviceName} service is disabled. IP: {ip}";
            SetErrorResponse(context, 403, errorMessage);
            PowerShellLog.Warn(errorMessage);

            return false;
        }

        private static bool CheckServiceAuthentication(HttpContext context, string serviceName, bool isAuthenticated)
        {
            if (isAuthenticated) return true;

            var ip = GetIp(context.Request);
            var errorMessage =
                $"[{serviceName}] The request could not be completed because the {serviceName} service requires authentication. IP: {ip}";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn(errorMessage);

            return false;
        }

        private static void RejectAuthenticationMethod(HttpContext context, string serviceName, string username = null, Exception ex = null)
        {
            var ip = GetIp(context.Request);
            var errorMessage = $"[{serviceName}] Unauthorized request to the {serviceName} service from IP {ip}, user: {username ?? "unknown"}, invalid credentials provided.";
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

            var ip = GetIp(context.Request);
            var errorMessage = $"[{serviceName}] The specified user {authUserName} is not authorized for the {serviceName} service. IP: {ip}";
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
                var relativePath = pathParam.TrimStart('/', '\\');
                if (Path.IsPathRooted(folder))
                {
                    folder = Path.GetFullPath(Path.Combine(folder, relativePath));
                }
                else
                {
                    folder = FileUtil.MapPath(StringUtil.EnsurePostfix('/', folder) + relativePath);
                }
            }

            return folder;
        }

        private static void ProcessFile(HttpContext context, string serviceName, bool isUpload, string originParam, string pathParam)
        {
            if (!string.IsNullOrEmpty(pathParam) && pathParam.Contains(".."))
            {
                var ip = GetIp(context.Request);
                PowerShellLog.Error($"[{serviceName}] Rejected file path with traversal attempt: '{pathParam}', IP: {ip}");
                SetErrorResponse(context, 403, "Path traversal is not allowed.");
                return;
            }

            if (isUpload)
            {
                ProcessFileUpload(context, serviceName, originParam, pathParam);
            }
            else
            {
                ProcessFileDownload(context, serviceName, originParam, pathParam);
            }
        }

        private static void ProcessFileUpload(HttpContext context, string serviceName, string originParam, string pathParam)
        {
            var file = GetPathFromParameters(originParam, pathParam);
            if (string.IsNullOrEmpty(file))
            {
                SetErrorResponse(context, 400, "Unable to resolve the upload path.");
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
                PowerShellLog.Info($"[{serviceName}] File uploaded: {fileInfo.Name}, size: {fileInfo.Length} bytes, path: {file}");
            }
            catch (UnauthorizedAccessException ex)
            {
                PowerShellLog.Error($"[{serviceName}] Write permission denied for path: {file}", ex);
                SetErrorResponse(context, 403, "Write access denied to the target path.");
            }
            catch (ArgumentException ex)
            {
                PowerShellLog.Error($"[{serviceName}] Invalid file path: {file}", ex);
                SetErrorResponse(context, 400, "The specified path contains invalid characters.");
            }
        }

        private static void ProcessFileDownload(HttpContext context, string serviceName, string originParam, string pathParam)
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
                PowerShellLog.Info($"[{serviceName}] File downloaded: {fileInfo.Name}, size: {fileInfo.Length} bytes, path: {file}");
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

        private static void ProcessMedia(HttpContext context, string serviceName, bool isUpload, Database scriptDb, string itemParam, bool unpackZip, bool skipExisting)
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
                    ms.Seek(0, SeekOrigin.Begin);
                    mc.CreateFromStream(ms, fileName, mco);
                    PowerShellLog.Info($"[{serviceName}] Media uploaded: {fileName}, size: {ms.Length} bytes, destination: {mco.Destination}");
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
                    PowerShellLog.Info($"[{serviceName}] Media updated: {mediaItem.Name}, size: {size} bytes, item: {mediaItem.ID}");
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
            PowerShellLog.Info($"[{serviceName}] Media downloaded: {mediaItem.Name}{str}, size: {mediaItem.Size} bytes, item: {mediaItem.ID}");
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

        private static void ProcessScript(HttpContext context, HttpRequest request, string serviceName, string outputFormat, string sessionId, bool persistentSession, bool useStructuredErrors = false)
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

            ProcessScript(context, script, serviceName, null, cliXmlArgs, outputFormat, sessionId, persistentSession, useStructuredErrors);
        }

        private static void ProcessScript(HttpContext context, Item scriptItem, string serviceName)
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

            ProcessScript(context, script, serviceName, streams);
        }

        private static void ProcessScript(HttpContext context, string script, string serviceName, Dictionary<string, Stream> streams, string cliXmlArgs = null, string outputFormat = "clixml", string sessionId = null, bool persistentSession = false, bool useStructuredErrors = false)
        {
            if (string.IsNullOrEmpty(script))
            {
                SetErrorResponse(context, 400, "The specified script is invalid.");
                return;
            }

            var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);
            var user = Sitecore.Context.User?.Name ?? "unknown";
            var ip = GetIp(context.Request);
            var scriptHash = ComputeScriptHash(script);
            var tokenResult = HttpContext.Current?.Items["SpeTokenResult"] as TokenValidationResult;
            var clientSession = tokenResult?.ClientSessionId ?? "none";

            PowerShellLog.Audit("Remoting: script starting, user={0}, ip={1}, session={2}, scriptHash={3}, clientSession={4}",
                user, ip, session.ID, scriptHash, clientSession);

            // Determine language mode restriction for user scripts (applied just before execution, restored after)
            var languageMode = WebServiceSettings.GetLanguageMode(serviceName);

            // Validate script against command restrictions
            var scope = tokenResult?.Scope;
            if (!ScriptValidator.ValidateScript(serviceName, script, scope, out var blockedCommand))
            {
                PowerShellLog.Audit("Remoting: script rejected, user={0}, ip={1}, blockedCommand={2}, clientSession={3}",
                    user, ip, blockedCommand, clientSession);
                SetErrorResponse(context, 403, $"Script contains blocked command: {blockedCommand}");
                return;
            }

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

                    if (languageMode != System.Management.Automation.PSLanguageMode.FullLanguage)
                        session.SetLanguageMode(languageMode);
                    try
                    {
                        session.ExecuteScriptPart(script, true);
                    }
                    finally
                    {
                        if (languageMode != System.Management.Automation.PSLanguageMode.FullLanguage)
                            session.SetLanguageMode(System.Management.Automation.PSLanguageMode.FullLanguage);
                    }

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

                    if (languageMode != System.Management.Automation.PSLanguageMode.FullLanguage)
                        session.SetLanguageMode(languageMode);
                    List<object> outObjects;
                    try
                    {
                        outObjects = session.ExecuteScriptPart(script, false, false, false) ?? new List<object>();
                    }
                    finally
                    {
                        if (languageMode != System.Management.Automation.PSLanguageMode.FullLanguage)
                            session.SetLanguageMode(System.Management.Automation.PSLanguageMode.FullLanguage);
                    }
                    var response = context.Response;
                    if (outputFormat.Equals("raw", StringComparison.OrdinalIgnoreCase))
                    {
                        // Raw output: no type information. Error streams use CliXml after delimiter.
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
                    else if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                    {
                        // JSON output: fast serialization preserving property names/values.
                        response.ContentType = "application/json";
                        session.SetVariable("outObjects", outObjects);

                        var errorObjects = new List<object>();
                        if (session.LastErrors != null && session.LastErrors.Any())
                        {
                            errorObjects.AddRange(session.LastErrors);
                        }
                        session.SetVariable("errorObjects", errorObjects);

                        session.Output.Clear();
                        session.ExecuteScriptPart(useStructuredErrors ? StructuredErrorScript : FlatErrorScript);

                        foreach (var outputBuffer in session.Output)
                        {
                            response.Write(outputBuffer.Text);
                        }
                    }
                    else
                    {
                        // CliXml output: full type-preserving serialization.
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

                PowerShellLog.Audit("Remoting: script completed, user={0}, ip={1}, session={2}, scriptHash={3}, hasErrors={4}, clientSession={5}",
                    user, ip, session.ID, scriptHash, session.Output.HasErrors, clientSession);
            }
            catch (Exception ex)
            {
                PowerShellLog.Audit("Remoting: script failed, user={0}, ip={1}, session={2}, scriptHash={3}, error={4}, clientSession={5}",
                    user, ip, session.ID, scriptHash, ex.GetType().Name, clientSession);
                PowerShellLog.Error("Error during script execution via RemoteScriptCall", ex);
                context.Response.StatusCode = 424;
                context.Response.StatusDescription = "Method Failure";

                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "application/json";
                    if (useStructuredErrors)
                    {
                        var errorBody = new Hashtable
                        {
                            ["output"] = new object[0],
                            ["errors"] = new[]
                            {
                                new Hashtable
                                {
                                    ["message"] = ex.Message,
                                    ["errorCategory"] = "NotSpecified",
                                    ["exceptionType"] = ex.GetType().FullName,
                                    ["exceptionMessage"] = ex.Message,
                                    ["scriptStackTrace"] = ex.StackTrace
                                }
                            }
                        };
                        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(errorBody));
                    }
                    else
                    {
                        var errorBody = new Hashtable
                        {
                            ["output"] = new object[0],
                            ["errors"] = new[] { ex.Message }
                        };
                        context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(errorBody));
                    }
                }
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