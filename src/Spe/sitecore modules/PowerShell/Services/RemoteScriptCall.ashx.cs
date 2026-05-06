using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.SessionState;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Security;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.Utility;
using Spe.sitecore_modules.PowerShell.Services.Processors;
using static Spe.sitecore_modules.PowerShell.Services.Processors.RemotingRequestContext;
using static Spe.sitecore_modules.PowerShell.Services.Processors.RemotingAuditFormat;
using static Spe.sitecore_modules.PowerShell.Services.Processors.ErrorResponseWriter;

namespace Spe.sitecore_modules.PowerShell.Services
{
    /// <summary>
    ///     The handler allows for execution of scripts stored within Script Library
    ///     it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : HttpTaskAsyncHandler, IRequiresSessionState
    {
        private const string ApiScriptsKey = "Spe.ApiScriptsKey";
        private const string ExpirationSetting = "Spe.WebApiCacheExpirationSecs";

        private const string ParamApiVersion = "apiVersion";
        private const string ParamScript = "script";
        private const string ParamPath = "path";
        private const string ParamScriptDb = "scriptDb";
        private const string ParamSessionId = "sessionId";
        private const string ParamPersistentSession = "persistentSession";
        private const string ParamRawOutput = "rawOutput";
        private const string ParamOutputFormat = "outputFormat";
        private const string ParamErrorFormat = "errorFormat";
        private const string ParamCaptureStreams = "captureStreams";

        private const string ParamAction = "action";
        private const string ParamSkipUnpack = "skipunpack";
        private const string ParamSkipExisting = "skipexisting";
        private const string ParamScDatabase = "sc_database";
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
            { "GET/wait" , WebServiceSettings.ServiceRemoting },
        };

        // Async entry point. Long-poll wait route gets true async handling;
        // every other route delegates to the sync ProcessRequest. Keeping
        // both paths means touching one handler class instead of deploying
        // a new .ashx for the async route.
        public override async Task ProcessRequestAsync(HttpContext context)
        {
            var apiVersion = context.Request.Params.Get(ParamApiVersion);
            var serviceMappingKey = context.Request.HttpMethod + "/" + apiVersion;
            if (string.Equals(serviceMappingKey, "GET/wait", StringComparison.OrdinalIgnoreCase))
            {
                await WaitProcessor.ProcessWaitAsync(context).ConfigureAwait(false);
                return;
            }
            ProcessRequest(context);
        }

        public override bool IsReusable => true;

        public override void ProcessRequest(HttpContext context)
        {
            var request = context.Request;
            var origin = request.Headers["Origin"];
            var apiVersion = request.Params.Get(ParamApiVersion);

            var serviceMappingKey = request.HttpMethod + "/" + apiVersion;
            if (!ApiVersionToServiceMapping.TryGetValue(serviceMappingKey, out var serviceName))
            {
                serviceName = string.Empty;
            }

            // endpoint is the api-version path token (script / wait / file /
            // media / handle / cleanup / test / 2), sourced from the ?api=
            // query param via ParamApiVersion. Sanitised so an unauthenticated
            // caller cannot inject arbitrary text into the audit log.
            var endpoint = LogSanitizer.SanitizeValue(apiVersion ?? "?");
            PowerShellLog.Audit($"[Remoting] action=requestReceived service={serviceName} endpoint={endpoint} ip={GetIp(request)} sessionId={TryReadAuditSessionId(request)} rid={GetRequestId()}");
            PowerShellLog.Debug($"[Remoting] action=requestUrl url={LogSanitizer.RedactUrl(request.Url)}");

            if (!AuthProcessor.CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthProcessor.AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }

            var authenticatedClient = HttpContext.Current?.Items[RemotingHttpContextKeys.RemotingClient] as RemotingClient;
            var tokenResult = HttpContext.Current?.Items[RemotingHttpContextKeys.TokenResult] as TokenValidationResult;
            PowerShellLog.Audit($"[Remoting] action=authenticated service={serviceName} endpoint={endpoint} ip={GetIp(request)} user={identity.Name} authKind={ResolveAuthKindForAudit(HttpContext.Current, request)} remotingClient={FormatRemotingClientId(authenticatedClient)} remotingClientKid={FormatRemotingClientKid(authenticatedClient)} clientId={FormatOAuthClientId(tokenResult)} sessionId={TryReadAuditSessionId(request)} rid={GetRequestId()}");

            DispatchRequest(context, request, apiVersion, serviceMappingKey, serviceName, identity, isAuthenticated);

            HttpResponseHelpers.AddCorsHeaders(context, serviceName, origin);
        }


        private static void DispatchRequest(HttpContext context, HttpRequest request, string apiVersion, string serviceMappingKey, string serviceName, AccountIdentity identity, bool isAuthenticated)
        {
            var requestParameters = request.Params;
            var itemParam = requestParameters.Get(ParamScript);
            var pathParam = requestParameters.Get(ParamPath);
            var originParam = requestParameters.Get(ParamScriptDb);
            var sessionId = requestParameters.Get(ParamSessionId);

            // Handle built-in actions without policy enforcement.
            var action = requestParameters.Get(ParamAction);
            if ("cleanup".Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                BuiltinActionProcessor.ProcessCleanup(context, sessionId, identity.Name);
                return;
            }

            if ("test".Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                BuiltinActionProcessor.ProcessTest(context, identity.Name);
                return;
            }

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
                PowerShellLog.Error($"[Remoting] action=databaseNotFound service={serviceMappingKey}");
                return;
            }

            Item scriptItem;

            // Every arm terminates with return: v1 and v2 resolve a scriptItem
            // and invoke the by-reference ScriptProcessor.ProcessScript inline,
            // the rest dispatch to a route-specific processor. A new case must
            // pick its own terminal action; there is no shared post-switch tail.
            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(itemParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + itemParam);
                    ScriptProcessor.ProcessScript(context, scriptItem, serviceName);
                    return;
                case "media":
                    MediaProcessor.ProcessMedia(context, serviceName, isUpload, scriptDb, itemParam, unpackZip, skipExisting);
                    return;
                case "file":
                    FileProcessor.ProcessFile(context, serviceName, isUpload, originParam, pathParam);
                    return;
                case "handle":
                    HandleProcessor.ProcessHandle(context, originParam);
                    return;
                case "2":
                    var apiScripts = GetApiScripts(dbName);
                    scriptItem = null;
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
                    ScriptProcessor.ProcessScript(context, scriptItem, serviceName);
                    return;
                case "script":
                    ScriptProcessor.ProcessScript(context, request, serviceName, outputFormat, sessionId, persistentSession, useStructuredErrors);
                    return;
                default:
                    PowerShellLog.Error($"[Remoting] action=unsupportedApiVersion service={serviceMappingKey}");
                    return;
            }
        }


        private static ApiScriptCollection GetApiScripts(string dbName)
        {
            Assert.ArgumentNotNullOrEmpty(dbName, "dbName");
            // Cache key must include dbName: the populated collection only
            // covers scripts for the db that triggered the miss, so sharing
            // one cache entry across databases silently 404s scripts that
            // exist in the other db until TTL expiry.
            var cacheKey = ApiScriptsKey + ":" + dbName;
            if (HttpRuntime.Cache[cacheKey] is ApiScriptCollection cachedScripts) return cachedScripts;

            lock (ApiScriptsLock)
            {
                if (HttpRuntime.Cache[cacheKey] is ApiScriptCollection doubleCheckScripts) return doubleCheckScripts;

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
                HttpRuntime.Cache.Add(cacheKey, apiScripts, null, Cache.NoAbsoluteExpiration,
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
                    PowerShellLog.Error("[Remoting] action=itemQueryFailed", ex);
                }
            }
        }

    }
}
