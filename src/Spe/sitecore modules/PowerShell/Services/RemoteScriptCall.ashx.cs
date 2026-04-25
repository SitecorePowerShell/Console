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
using System.Threading;
using System.Threading.Tasks;
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
    public class RemoteScriptCall : HttpTaskAsyncHandler, IRequiresSessionState
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
        private const string ParamCaptureStreams = "captureStreams";

        // Redefines the five Write-* stream cmdlets so their streams route
        // into the output pipeline (captured in the CliXml response) instead
        // of being swallowed server-side. Injected only when the client
        // signals captureStreams=true. Prepended after policy scanning so
        // the allowlist scanner never sees the inner module-qualified calls.
        private const string StreamCaptureBootstrap =
            "if($PSVersionTable.PSVersion.Major -ge 5) {" +
            "    function Write-Information { param([string]$Message) $InformationPreference = \"Continue\"; Microsoft.PowerShell.Utility\\Write-Information -Message $Message 6>&1 }" +
            "};" +
            "function Write-Debug { param([string]$Message) $DebugPreference = \"Continue\"; Microsoft.PowerShell.Utility\\Write-Debug -Message $Message 5>&1 };" +
            "function Write-Verbose { param([string]$Message) $VerbosePreference = \"Continue\"; Microsoft.PowerShell.Utility\\Write-Verbose -Message $Message 4>&1 };" +
            "function Write-Warning { param([string]$Message) $WarningPreference = \"Continue\"; Microsoft.PowerShell.Utility\\Write-Warning -Message $Message 3>&1 };" +
            "function Write-Error { param([string]$Message) $WarningPreference = \"Continue\"; Microsoft.PowerShell.Utility\\Write-Error -Message $Message 2>&1 };";

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
        private const string ParamAction = "action";
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
                await ProcessWaitAsync(context).ConfigureAwait(false);
                return;
            }
            ProcessRequest(context);
        }

        public override bool IsReusable => true;

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

            PowerShellLog.Audit($"[Remoting] action=requestReceived service={serviceName} ip={GetIp(request)} rid={GetRequestId()}");
            PowerShellLog.Debug($"[Remoting] action=requestUrl url={LogSanitizer.RedactUrl(request.Url)}");

            if (!CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }

            var authenticatedClient = HttpContext.Current?.Items["SpeRemotingClient"] as RemotingClient;
            PowerShellLog.Audit($"[Remoting] action=authenticated service={serviceName} ip={GetIp(request)} user={identity.Name} remotingClient={authenticatedClient?.Name ?? "none"} rid={GetRequestId()}");

            DispatchRequest(context, request, apiVersion, serviceMappingKey, serviceName, identity, isAuthenticated);

            AddCorsHeaders(context, serviceName, origin);
        }

        private static void AddThrottleHeaders(HttpResponse response, RemotingClientProvider.ThrottleResult throttle)
        {
            if (!throttle.HasLimit) return;

            response.Headers["X-RateLimit-Limit"] = throttle.Limit.ToString();
            response.Headers["X-RateLimit-Remaining"] = throttle.Remaining.ToString();
            response.Headers["X-RateLimit-Reset"] = throttle.ResetUnixTimestamp.ToString();

            if (!throttle.Allowed)
            {
                response.Headers["Retry-After"] = throttle.RetryAfterSeconds.ToString();
            }
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
                PowerShellLog.Warn($"[Remoting] action=deprecatedAuth service={serviceName} ip={GetIp(request)} user={username} reason=queryStringCredentials");
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
                    // Populated with expired|disabled|invalid when Path A rejects the request
                    // so the 401 response can surface it in X-SPE-AuthFailureReason.
                    string authFailureReason = null;
                    try
                    {
                        TokenValidationResult tokenResult = null;
                        RemotingClient matchedClient = null;
                        var isValid = false;
                        string authKind = null;

                        var authority = request.Url.GetLeftPart(UriPartial.Authority);

                        // Algorithm-based dispatch (RFC 8725). Each provider owns
                        // a disjoint algorithm family; the handler picks one based
                        // on the JWT header, never fallthrough between providers.
                        //
                        //   HS256 / HS384 / HS512         -> SharedSecret provider
                        //   RS256 / RS384 / RS512 / ES*   -> first OAuth provider
                        //                                    that accepts the alg
                        var alg = SharedSecretAuthenticationProvider.ExtractAlgorithm(token);

                        if (!string.IsNullOrEmpty(alg) && alg.StartsWith("HS", StringComparison.Ordinal))
                        {
                            // ---- SharedSecret provider (HMAC) ----
                            // Iterate providers in reverse so entries under the plural
                            // <authenticationProviders> list take priority over the
                            // singular back-compat <authenticationProvider> slot. An
                            // operator who adds a plural SharedSecret entry (whether
                            // or not they delete the singular) has expressed a more
                            // deliberate intent than the default config; the plural
                            // entry wins when both accept the same alg family.
                            SharedSecretAuthenticationProvider sharedSecretProvider = null;
                            var providers = ServiceAuthenticationManager.AuthenticationProviders;
                            for (int i = providers.Count - 1; i >= 0; i--)
                            {
                                if (providers[i] is SharedSecretAuthenticationProvider ss)
                                {
                                    sharedSecretProvider = ss;
                                    break;
                                }
                            }
                            if (sharedSecretProvider != null)
                            {
                                var kid = SharedSecretAuthenticationProvider.ExtractKeyId(token);

                                if (!string.IsNullOrEmpty(kid))
                                {
                                    authKind = "sharedSecretClient";

                                    // Path A: per-item Shared Secret Client lookup by Access Key Id.
                                    if (!RemotingClientProvider.IsRegistryLoaded())
                                    {
                                        PowerShellLog.Warn($"[Remoting] action=coldStart ip={ip} rid={GetRequestId()}");
                                        context.Response.AddHeader("Retry-After", "2");
                                        SetErrorResponse(context, 503, "Service is starting. Please retry.");
                                        return false;
                                    }

                                    matchedClient = RemotingClientProvider.FindByAccessKeyId(kid);
                                    if (matchedClient != null)
                                    {
                                        isValid = sharedSecretProvider.ValidateToken(
                                            token, authority, out username, out tokenResult,
                                            matchedClient.SharedSecret, skipUsernameValidation: true);

                                        if (!isValid)
                                        {
                                            PowerShellLog.Debug($"[Remoting] action=clientSignatureFailed kid={kid} remotingClient={matchedClient.Name}");
                                            authFailureReason = RemotingClientProvider.AuthFailureReasonInvalid;
                                            matchedClient = null;
                                        }
                                    }
                                    else
                                    {
                                        authFailureReason = RemotingClientProvider.GetAuthFailureReason(kid)
                                            ?? RemotingClientProvider.AuthFailureReasonInvalid;
                                        PowerShellLog.Debug($"[Remoting] action=clientNotFound kid={kid} reason={authFailureReason}");
                                    }
                                }
                                else
                                {
                                    // Path B: No kid - config-level shared-secret validation.
                                    // Intended for trusted automation. Not rate-limited.
                                    authKind = "configSecret";
                                    try
                                    {
                                        isValid = sharedSecretProvider.Validate(token, authority, out username, out tokenResult);
                                    }
                                    catch (SecurityException)
                                    {
                                        PowerShellLog.Debug("[Remoting] action=configSecretFailed");
                                    }
                                }
                            }
                        }
                        else if (!string.IsNullOrEmpty(alg))
                        {
                            // ---- OAuth bearer provider (asymmetric) ----
                            // Route by iss to support multiple IdPs side by side
                            // (e.g. Sitecore IDS + Auth0, both signing with RS256).
                            // SelectProvider picks the OAuth provider whose
                            // AllowedIssuers contains the token's iss AND whose
                            // AllowedAlgorithms accepts the token's alg, so an
                            // RS-signed token can't route through an ES-only
                            // provider. Tokens with no iss or an unrecognised iss
                            // return null and fall through to 401.
                            var providers = ServiceAuthenticationManager.AuthenticationProviders;
                            OAuthBearerTokenAuthenticationProvider oauthProvider = null;
                            if (OAuthBearerTokenAuthenticationProvider.TryPeekIssuer(token, out var peekedIssuer))
                            {
                                oauthProvider = OAuthBearerTokenAuthenticationProvider.SelectProvider(
                                    providers, alg, peekedIssuer);
                            }

                            if (oauthProvider != null)
                            {
                                authKind = "oauthClient";
                                try
                                {
                                    isValid = oauthProvider.Validate(token, authority, out username, out tokenResult);
                                }
                                catch (SecurityException)
                                {
                                    PowerShellLog.Debug("[Remoting] action=oauthValidationFailed");
                                }

                                if (!isValid && !string.IsNullOrEmpty(tokenResult?.FailureReason))
                                {
                                    authFailureReason = tokenResult.FailureReason;
                                }

                                if (isValid)
                                {
                                    var issuer = tokenResult?.Issuer;
                                    var resolvedCid = tokenResult?.ClientId;
                                    matchedClient = RemotingClientProvider.FindByIssuerAndClientId(issuer, resolvedCid);

                                    if (matchedClient == null)
                                    {
                                        PowerShellLog.Debug($"[Remoting] action=clientNotFound issuer={LogSanitizer.SanitizeValue(issuer)} clientId={LogSanitizer.SanitizeValue(resolvedCid)}");
                                        authFailureReason = RemotingClientProvider.AuthFailureReasonInvalid;
                                        isValid = false;
                                    }
                                }
                            }
                            else
                            {
                                PowerShellLog.Debug($"[Remoting] action=bearerProviderMissing alg={LogSanitizer.SanitizeValue(alg)} iss={LogSanitizer.SanitizeValue(peekedIssuer ?? "none")}");
                            }
                        }
                        else
                        {
                            // Missing or unrecognised alg - fall through to 401.
                            PowerShellLog.Debug("[Remoting] action=unrecognizedAlg");
                        }

                        if (isValid)
                        {
                            // Remoting Clients require an Impersonated User for identity
                            if (matchedClient != null)
                            {
                                if (!matchedClient.HasImpersonation)
                                {
                                    PowerShellLog.Audit("[Remoting] action=clientDenied reason=noImpersonateUser remotingClient={0} ip={1} rid={2}",
                                        matchedClient.Name, ip, GetRequestId());
                                    SetErrorResponse(context, 403, "This Remoting Client requires an Impersonated User. Configure the field on the client item.");
                                    return false;
                                }

                                username = matchedClient.ImpersonateUser;
                            }

                            // Throttle check
                            if (matchedClient != null)
                            {
                                var throttle = RemotingClientProvider.CheckThrottle(matchedClient);
                                AddThrottleHeaders(context.Response, throttle);

                                if (!throttle.Allowed)
                                {
                                    var throttlePolicy = RemotingPolicyManager.ResolvePolicy(matchedClient.Policy);
                                    if (string.Equals(throttle.Action, "Bypass", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (throttlePolicy.AuditLevel >= AuditLevel.Standard)
                                        {
                                            PowerShellLog.Audit("[Remoting] action=throttleBypassed remotingClient={0} ip={1} rid={2}",
                                                matchedClient.Name, ip, GetRequestId());
                                        }
                                    }
                                    else
                                    {
                                        if (throttlePolicy.AuditLevel >= AuditLevel.Violations)
                                        {
                                            PowerShellLog.Audit("[Remoting] action=throttled remotingClient={0} ip={1} rid={2}",
                                                matchedClient.Name, ip, GetRequestId());
                                        }
                                        SetErrorResponse(context, 429, "Rate limit exceeded.");
                                        return false;
                                    }
                                }
                            }

                            authenticationManager.SwitchToUser(username, true);
                            PowerShellLog.Debug($"[Remoting] action=bearerAuthSuccess authKind={authKind ?? "none"} user={username} ip={ip} clientSession={tokenResult?.ClientSessionId ?? "none"} remotingClient={matchedClient?.Name ?? "none"} clientId={LogSanitizer.SanitizeValue(tokenResult?.ClientId ?? "none")}");
                            if (tokenResult != null)
                            {
                                HttpContext.Current.Items["SpeTokenResult"] = tokenResult;
                            }
                            if (matchedClient != null)
                            {
                                HttpContext.Current.Items["SpeRemotingClient"] = matchedClient;
                            }
                        }
                        else
                        {
                            PowerShellLog.Audit("[Remoting] action=bearerAuthFailed authKind={0} user={1} ip={2} remotingClient={3} clientId={4} rid={5}",
                                authKind ?? "none", username ?? "unknown", ip, matchedClient?.Name ?? "none",
                                LogSanitizer.SanitizeValue(tokenResult?.ClientId ?? "none"), GetRequestId());
                            context.Response.AddHeader("WWW-Authenticate", JwtClaimValidator.BuildWwwAuthenticate(authFailureReason));
                            if (!string.IsNullOrEmpty(authFailureReason))
                            {
                                context.Response.AddHeader("X-SPE-AuthFailureReason", authFailureReason);
                            }
                            RejectAuthenticationMethod(context, serviceName, username);
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        PowerShellLog.Audit("[Remoting] action=bearerAuthError user={0} ip={1} error={2} rid={3}", username ?? "unknown", ip, LogSanitizer.SanitizeValue(ex.Message), GetRequestId());
                        context.Response.AddHeader("WWW-Authenticate", JwtClaimValidator.BuildWwwAuthenticate(authFailureReason));
                        if (!string.IsNullOrEmpty(authFailureReason))
                        {
                            context.Response.AddHeader("X-SPE-AuthFailureReason", authFailureReason);
                        }
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

            // Handle built-in actions without policy enforcement.
            var action = requestParameters.Get(ParamAction);
            if ("cleanup".Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                ProcessCleanup(context, sessionId, identity.Name);
                return;
            }

            if ("test".Equals(action, StringComparison.OrdinalIgnoreCase))
            {
                ProcessTest(context, identity.Name);
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
                    PowerShellLog.Error($"[Remoting] action=unsupportedApiVersion service={serviceMappingKey}");
                    return;
            }

            ProcessScript(context, scriptItem, serviceName);
        }

        private const string ClientIpKey = "SpeClientIp";

        private static string GetIp(HttpRequest request)
        {
            // Cached per-request: GetIp is called 15+ times along the pipeline
            // and every uncached call touches ServerVariables, which is a
            // lazy-populated collection backed by IIS worker-process state.
            var ctx = HttpContext.Current;
            if (ctx != null && ctx.Items[ClientIpKey] is string cached) return cached;

            var ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (string.IsNullOrEmpty(ip))
            {
                ip = request.ServerVariables["REMOTE_ADDR"];
            }

            if (ctx != null && !string.IsNullOrEmpty(ip))
            {
                ctx.Items[ClientIpKey] = ip;
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

        private const string RequestIdKey = "SpeRequestId";

        /// <summary>
        /// Returns the correlation ID for the current HTTP request, generating one if needed.
        /// </summary>
        private static string GetRequestId()
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return "no-ctx";
            var rid = ctx.Items[RequestIdKey] as string;
            if (rid != null) return rid;
            rid = Guid.NewGuid().ToString("N").Substring(0, 8);
            ctx.Items[RequestIdKey] = rid;
            return rid;
        }

        /// <summary>
        /// Resolves the active remoting policy from the current HTTP context.
        /// Returns <see cref="RemotingPolicy.Unrestricted"/> when no Remoting Client is present
        /// (config-based shared-secret sessions).
        /// </summary>
        private static RemotingPolicy GetActivePolicy()
        {
            var client = HttpContext.Current?.Items["SpeRemotingClient"] as RemotingClient;
            if (client == null) return RemotingPolicy.Unrestricted;
            return RemotingPolicyManager.ResolvePolicy(client.Policy);
        }

        // Build the ownership identity string for the current request. Format
        // "remotingClient:<name>" when a Remoting Client authenticated the request,
        // otherwise "user:<username>" for config-based shared-secret or Basic auth.
        private static string GetRequestOwnershipIdentity(HttpContext context)
        {
            var client = HttpContext.Current?.Items["SpeRemotingClient"] as RemotingClient;
            if (client != null)
            {
                return "remotingClient:" + client.Name;
            }
            var user = Sitecore.Context.User?.Name;
            return string.IsNullOrEmpty(user) ? null : "user:" + user;
        }

        // Claim the session on first access, enforce ownership afterwards.
        // Returns true to continue processing, false if ownership check failed
        // and an error response has already been written.
        private const string SessionOwnerCacheKey = "SPE.SessionOwner|";
        private static readonly TimeSpan SessionOwnerTtl = TimeSpan.FromMinutes(30);

        // Claim the session on first access, enforce ownership afterwards.
        // Ownership is stored in HttpRuntime.Cache keyed on the client-supplied
        // sessionId so the check is stable across requests regardless of ASP.NET
        // session-cookie lifecycle. Mirrored onto ScriptSession.CreatedByIdentity
        // for observability by in-process consumers.
        private static bool TryEnforceSessionOwnership(HttpContext context, ScriptSession session, string ip)
        {
            if (session == null) return true;

            var identity = GetRequestOwnershipIdentity(context);
            if (string.IsNullOrEmpty(identity)) return true;

            var clientSessionId = session.ID;
            if (string.IsNullOrEmpty(clientSessionId)) return true;

            var cacheKey = SessionOwnerCacheKey + clientSessionId;
            var recordedOwner = HttpRuntime.Cache.Get(cacheKey) as string;

            if (string.IsNullOrEmpty(recordedOwner))
            {
                HttpRuntime.Cache.Insert(cacheKey, identity, null,
                    DateTime.UtcNow.Add(SessionOwnerTtl),
                    System.Web.Caching.Cache.NoSlidingExpiration);
                session.CreatedByIdentity = identity;
                return true;
            }

            if (string.Equals(recordedOwner, identity, StringComparison.Ordinal))
            {
                if (string.IsNullOrEmpty(session.CreatedByIdentity))
                {
                    session.CreatedByIdentity = recordedOwner;
                }
                return true;
            }

            PowerShellLog.Audit("[Remoting] action=sessionOwnershipMismatch session={0} owner={1} caller={2} ip={3} rid={4}",
                clientSessionId, recordedOwner, identity, ip, GetRequestId());
            context.Response.Headers["X-SPE-Restriction"] = "session-not-owned";
            SetErrorResponse(context, 403, "Session is owned by a different identity.");
            return false;
        }

        // GET /-/script/wait/ long-poll endpoint. Holds the request until the
        // target job (or script session) transitions to done, or until the
        // caller-provided timeoutSeconds (clamped 1..60) elapses. Returns
        // JSON so clients can read status without a PS runspace. The
        // endpoint is throttled, authenticated, and access-controlled via
        // the same AuthenticateRequest pipeline as every other route.
        private async Task ProcessWaitAsync(HttpContext context)
        {
            var request = context.Request;
            var serviceName = WebServiceSettings.ServiceRemoting;
            var origin = request.Headers["Origin"];

            PowerShellLog.Audit($"[Remoting] action=waitRequestReceived service={serviceName} ip={GetIp(request)} rid={GetRequestId()}");

            if (!CheckServiceEnabled(context, serviceName))
            {
                return;
            }

            if (!AuthenticateRequest(context, serviceName, out var identity, out var isAuthenticated))
            {
                return;
            }
            if (!isAuthenticated || identity == null)
            {
                RejectAuthenticationMethod(context, serviceName, null);
                return;
            }

            var sessionId = request.QueryString[ParamSessionId];
            var jobId = request.QueryString["jobId"];
            var jobType = request.QueryString["jobType"] ?? "scriptsession";
            var timeoutSecondsRaw = request.QueryString["timeoutSeconds"];

            if (string.IsNullOrEmpty(jobId))
            {
                SetErrorResponse(context, 400, "jobId is required.");
                return;
            }

            int timeoutSeconds = 30;
            if (!string.IsNullOrEmpty(timeoutSecondsRaw) && int.TryParse(timeoutSecondsRaw, out var parsed))
            {
                timeoutSeconds = parsed;
            }
            if (timeoutSeconds < 1) timeoutSeconds = 1;
            if (timeoutSeconds > 60) timeoutSeconds = 60;

            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            var cancellationToken = context.Response.ClientDisconnectedToken;
            var started = DateTime.UtcNow;

            string status = "NotFound";
            string name = jobId;
            bool isDone = true;

            while (DateTime.UtcNow < deadline)
            {
                if (cancellationToken.IsCancellationRequested) return;

                if (string.Equals(jobType, "sitecore", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryProbeSitecoreJob(jobId, out status, out name, out isDone))
                    {
                        if (isDone) break;
                    }
                    else
                    {
                        // Unknown handle - uniform response (no 404) to resist enumeration.
                        status = "NotFound";
                        isDone = true;
                        break;
                    }
                }
                else if (string.Equals(jobType, "scriptsession", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryProbeScriptSession(context, sessionId, jobId, out status, out name, out isDone, out var ownershipRejected))
                    {
                        if (ownershipRejected) return;
                        if (isDone) break;
                    }
                    else
                    {
                        status = "NotFound";
                        isDone = true;
                        break;
                    }
                }
                else
                {
                    SetErrorResponse(context, 400, "Unsupported jobType. Expected 'scriptsession' or 'sitecore'.");
                    return;
                }

                try
                {
                    await Task.Delay(200, cancellationToken).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }

            var elapsed = (int)Math.Round((DateTime.UtcNow - started).TotalSeconds);
            var json = "{\"isDone\":" + (isDone ? "true" : "false") +
                       ",\"status\":\"" + JsonEscape(status) + "\"" +
                       ",\"name\":\"" + JsonEscape(name ?? jobId) + "\"" +
                       ",\"elapsedSeconds\":" + elapsed + "}";
            context.Response.ContentType = "application/json";
            context.Response.Write(json);

            AddCorsHeaders(context, serviceName, origin);
        }

        private static bool TryProbeSitecoreJob(string jobHandle, out string status, out string name, out bool isDone)
        {
            status = "NotFound"; name = jobHandle; isDone = true;
            try
            {
                var handle = Sitecore.Handle.Parse(jobHandle);
                if (handle == null) return false;
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager?.GetJob(handle);
                if (job == null) return false;
                name = job.Name ?? jobHandle;
                isDone = job.IsDone || job.StatusFailed;
                status = job.StatusFailed ? "Failed" : (job.IsDone ? "Done" : "Busy");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryProbeScriptSession(HttpContext context, string sessionId, string jobSessionId, out string status, out string name, out bool isDone, out bool ownershipRejected)
        {
            status = "NotFound"; name = jobSessionId; isDone = true; ownershipRejected = false;

            // Background -AsJob sessions are keyed with the creating request's
            // ASP.NET session id - which the current wait request will not share.
            // Find the session across any user-session by matching on just the
            // client-supplied persistent id.
            var targetSession = ScriptSessionManager.GetMatchingSessionsForAnyUserSession(jobSessionId).FirstOrDefault();
            if (targetSession == null)
            {
                targetSession = ScriptSessionManager.GetSessionIfExists(jobSessionId);
            }
            if (targetSession == null) return false;

            // Ownership check uses the caller's own sessionId (the outer remoting
            // session). If omitted, fall back to the job session.
            ScriptSession ownerCheckSession = null;
            if (!string.IsNullOrEmpty(sessionId))
            {
                ownerCheckSession = ScriptSessionManager.GetMatchingSessionsForAnyUserSession(sessionId).FirstOrDefault()
                    ?? ScriptSessionManager.GetSessionIfExists(sessionId);
            }
            if (ownerCheckSession == null) ownerCheckSession = targetSession;

            if (!TryEnforceSessionOwnership(context, ownerCheckSession, GetIp(context.Request)))
            {
                ownershipRejected = true;
                return true;
            }

            name = targetSession.ID ?? jobSessionId;
            var state = targetSession.State;
            isDone = state != System.Management.Automation.Runspaces.RunspaceAvailability.Busy;
            status = isDone ? "Idle" : "Busy";
            return true;
        }

        private static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

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
            var errorMessage = $"The {serviceName} service is disabled.";
            SetErrorResponse(context, 403, errorMessage);
            PowerShellLog.Warn($"[Remoting] action=serviceDisabled service={serviceName} ip={ip}");

            return false;
        }

        /// <summary>
        /// Attempts to validate a Bearer token against each enabled Shared Secret Client's shared secret.
        /// Returns the matched client on success, or null if no match.
        /// Sets <paramref name="registryAvailable"/> to false when the client registry
        /// could not be loaded (e.g. during cold start).
        /// </summary>



        private static bool CheckServiceAuthentication(HttpContext context, string serviceName, bool isAuthenticated)
        {
            if (isAuthenticated) return true;

            var ip = GetIp(context.Request);
            var errorMessage = $"The {serviceName} service requires authentication.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn($"[Remoting] action=authRequired service={serviceName} ip={ip}");

            return false;
        }

        private static void RejectAuthenticationMethod(HttpContext context, string serviceName, string username = null, Exception ex = null)
        {
            var ip = GetIp(context.Request);
            var errorMessage = $"Unauthorized request to the {serviceName} service.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Audit($"[Remoting] action=authRejected service={serviceName} ip={ip} user={username ?? "unknown"} rid={GetRequestId()}");

            if (ex != null)
            {
                context.Response.StatusDescription += $" {ex.Message}";
                PowerShellLog.Error($"[Remoting] action=authRejected error={ex.Message}");
            }
        }

        private static bool CheckIsUserAuthorized(HttpContext context, string authUserName, string serviceName)
        {
            var isAuthorized = ServiceAuthorizationManager.IsUserAuthorized(serviceName, authUserName);
            if (isAuthorized) return true;

            var ip = GetIp(context.Request);
            var errorMessage = $"The specified user is not authorized for the {serviceName} service.";
            SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Audit($"[Remoting] action=userUnauthorized service={serviceName} ip={ip} user={authUserName} rid={GetRequestId()}");

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
                PowerShellLog.Audit($"[Remoting] action=pathTraversalBlocked service={serviceName} ip={ip} path=\"{pathParam}\" rid={GetRequestId()}");
                PowerShellLog.Error($"[Remoting] action=pathTraversalBlocked service={serviceName} ip={ip}");
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
                if (GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                {
                    PowerShellLog.Audit($"[Remoting] action=fileUploaded service={serviceName} file={fileInfo.Name} size={fileInfo.Length} path=\"{file}\" rid={GetRequestId()}");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                PowerShellLog.Error($"[Remoting] action=writePermissionDenied service={serviceName} path=\"{file}\"", ex);
                SetErrorResponse(context, 403, "Write access denied to the target path.");
            }
            catch (ArgumentException ex)
            {
                PowerShellLog.Error($"[Remoting] action=invalidFilePath service={serviceName} path=\"{file}\"", ex);
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
                if (GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                {
                    PowerShellLog.Audit($"[Remoting] action=fileDownloaded service={serviceName} file={fileInfo.Name} size={fileInfo.Length} path=\"{file}\" rid={GetRequestId()}");
                }
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
                    PowerShellLog.Warn($"[Remoting] action=undeterminedFilename entry={fileName}");
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
                    if (GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                    {
                        PowerShellLog.Audit($"[Remoting] action=mediaUploaded service={serviceName} file={fileName} size={ms.Length} destination=\"{mco.Destination}\" rid={GetRequestId()}");
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
                    if (GetActivePolicy().AuditLevel >= AuditLevel.Violations)
                    {
                        PowerShellLog.Audit($"[Remoting] action=mediaUpdated service={serviceName} item=\"{mediaItem.Name} {mediaItem.ID}\" size={size} rid={GetRequestId()}");
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
            if (GetActivePolicy().AuditLevel >= AuditLevel.Violations)
            {
                PowerShellLog.Audit($"[Remoting] action=mediaDownloaded service={serviceName} item=\"{mediaItem.Name}{str} {mediaItem.ID}\" size={mediaItem.Size} rid={GetRequestId()}");
            }
            WebUtil.TransmitStream(mediaStream, context.Response, Settings.Media.StreamBufferSize);
        }

        private static string ReadRequestBody(HttpRequest request)
        {
            // Pre-size to ContentLength (when known) so CopyTo does not re-grow
            // the internal buffer; then use GetBuffer()+Length to skip the
            // extra full-body copy that ToArray() performs. For gzip bodies
            // StreamReader pulls decoded chars straight off GZipStream, so no
            // intermediate byte[] is materialised either.
            var capacity = request.ContentLength > 0 ? request.ContentLength : 0;
            using (var ms = capacity > 0 ? new MemoryStream(capacity) : new MemoryStream())
            {
                request.InputStream.CopyTo(ms);
                var length = (int)ms.Length;
                var buffer = ms.GetBuffer();

                var shouldDecompress = request.Headers["Content-Encoding"]?.Contains("gzip") ?? false;
                if (shouldDecompress)
                {
                    using (var inputMs = new MemoryStream(buffer, 0, length, writable: false))
                    using (var gz = new GZipStream(inputMs, CompressionMode.Decompress))
                    using (var reader = new StreamReader(gz, Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }

                return Encoding.UTF8.GetString(buffer, 0, length);
            }
        }

        private static void ProcessCleanup(HttpContext context, string sessionId, string user)
        {
            var ip = GetIp(context.Request);

            if (string.IsNullOrEmpty(sessionId))
            {
                SetErrorResponse(context, 400, "Session ID is required for cleanup.");
                return;
            }

            var disposed = 0;
            foreach (var session in ScriptSessionManager.GetMatchingSessionsForAnyUserSession(sessionId).ToList())
            {
                if (session.ApplianceType == "RemoteAutomation")
                {
                    ScriptSessionManager.RemoveSession(session);
                    disposed++;
                }
            }

            if (GetActivePolicy().AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=sessionCleanup user={0} ip={1} sessionId={2} disposed={3} rid={4}",
                    user, ip, sessionId, disposed, GetRequestId());
            }

            context.Response.StatusCode = 200;
            context.Response.ContentType = "text/plain";
            context.Response.Write($"Disposed {disposed} session(s).");
        }

        private static void ProcessTest(HttpContext context, string user)
        {
            var ip = GetIp(context.Request);

            if (GetActivePolicy().AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=connectionTest user={0} ip={1} rid={2}", user, ip, GetRequestId());
            }

            var result = new
            {
                SPEVersion = CurrentVersion.SpeVersion.ToString(),
                SitecoreVersion = SitecoreVersion.Current.ToString(),
                CurrentTime = DateTime.UtcNow.ToString("o")
            };

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.Write(Newtonsoft.Json.JsonConvert.SerializeObject(result));
        }

        private static void ProcessScript(HttpContext context, HttpRequest request, string serviceName, string outputFormat, string sessionId, bool persistentSession, bool useStructuredErrors = false)
        {
            if (request?.InputStream == null) return;

            string script = null;
            string cliXmlArgs = null;
            var requestBody = ReadRequestBody(request);
            var splitBody = requestBody.Split(new[] { $"<#{sessionId}#>" }, StringSplitOptions.RemoveEmptyEntries);
            if (splitBody.Length > 0)
            {
                script = splitBody[0];
                if (splitBody.Length > 1)
                {
                    cliXmlArgs = splitBody[1];
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

            ProcessScript(context, script, serviceName, streams, scriptItemId: scriptItem.ID, isInlineScript: false);
        }

        private static void ProcessScript(HttpContext context, string script, string serviceName, Dictionary<string, Stream> streams, string cliXmlArgs = null, string outputFormat = "clixml", string sessionId = null, bool persistentSession = false, bool useStructuredErrors = false, ID scriptItemId = null, bool isInlineScript = true)
        {
            if (string.IsNullOrEmpty(script))
            {
                SetErrorResponse(context, 400, "The specified script is invalid.");
                return;
            }

            var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);
            var user = Sitecore.Context.User?.Name ?? "unknown";
            var ip = GetIp(context.Request);
            // Lazy: the hash is only consumed inside AuditLevel >= Standard
            // guards. Skip the SHA256 + hex conversion entirely when the
            // active policy runs at None or Violations.
            var scriptHash = new Lazy<string>(() => ComputeScriptHash(script));
            var tokenResult = HttpContext.Current?.Items["SpeTokenResult"] as TokenValidationResult;
            var clientSession = tokenResult?.ClientSessionId ?? "none";

            // Session ownership: first caller claims the session; subsequent
            // calls with a different identity are rejected. Prevents a second
            // authenticated principal (e.g. a different Remoting Client) from
            // attaching to a session id they did not create.
            if (!TryEnforceSessionOwnership(context, session, ip))
            {
                return;
            }

            if (GetActivePolicy().AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=scriptStarting user={0} ip={1} session={2} scriptHash={3} clientSession={4} rid={5}",
                    user, ip, session.ID, scriptHash.Value, clientSession, GetRequestId());
            }

            // Language mode defaults to FullLanguage; policy may restrict it below
            var languageMode = System.Management.Automation.PSLanguageMode.FullLanguage;

            // Resolve remoting policy (Remoting Client > unrestricted for config-based shared-secret)
            var client = HttpContext.Current?.Items["SpeRemotingClient"] as RemotingClient;

            RemotingPolicy policy;
            if (client != null)
            {
                if (!client.HasPolicy)
                {
                    PowerShellLog.Audit(
                        "[Remoting] action=clientDenied reason=noPolicyAssigned remotingClient={0} user={1} ip={2} rid={3}",
                        client.Name, user, ip, GetRequestId());
                    SetErrorResponse(context, 403, "The request is not authorized. A remoting policy is required.");
                    return;
                }

                policy = RemotingPolicyManager.ResolvePolicy(client.Policy);
            }
            else
            {
                // Config-based shared-secret sessions have no client item - unrestricted
                policy = RemotingPolicy.Unrestricted;
            }

            // Policy-based validation
            if (policy != null && policy != RemotingPolicy.Unrestricted)
            {
                if (isInlineScript)
                {
                    // Inline scripts (remoting endpoint): enforce command allowlist
                    if (!ScriptValidator.ValidateScriptAgainstPolicy(policy, script, out var policyBlockedCommand))
                    {
                        if (policy.AuditLevel >= AuditLevel.Violations)
                        {
                            PowerShellLog.Audit("[Remoting] action=scriptRejectedByPolicy user={0} ip={1} policy={2} blockedCommand={3} clientSession={4} rid={5}",
                                user, ip, policy.Name, policyBlockedCommand, clientSession, GetRequestId());
                        }
                        context.Response.Headers["X-SPE-Restriction"] = "policy-blocked";
                        context.Response.Headers["X-SPE-BlockedCommand"] = policyBlockedCommand;
                        context.Response.Headers["X-SPE-Policy"] = policy.Name;
                        SetErrorResponse(context, 403, $"Script blocked by remoting policy '{policy.Name}': {policyBlockedCommand}");
                        return;
                    }
                }
                else
                {
                    // By-reference scripts (v2 endpoint): enforce approved scripts list
                    if (!policy.IsScriptApproved(scriptItemId))
                    {
                        if (policy.AuditLevel >= AuditLevel.Violations)
                        {
                            PowerShellLog.Audit(
                                "[Remoting] action=scriptNotApproved script={0} policy={1} user={2} ip={3} rid={4}",
                                scriptItemId, policy.Name, user, ip, GetRequestId());
                        }
                        SetErrorResponse(context, 403, "The requested script is not approved under this policy.");
                        return;
                    }

                    if (policy.AuditLevel >= AuditLevel.Standard)
                    {
                        PowerShellLog.Audit(
                            "[Approval] action=scriptApproved script={0} policy={1} rid={2}",
                            scriptItemId, policy.Name, GetRequestId());
                    }
                }

                // Policy's language mode applies to all scripts (approved and unapproved)
                if (policy.LanguageMode > languageMode)
                {
                    languageMode = policy.LanguageMode;
                }

                if (policy.AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=policyAudit service={0} policy={1} scriptHash={2} clientSession={3} endpoint={4} rid={5}",
                        serviceName, policy.Name, scriptHash.Value, clientSession, isInlineScript ? "inline" : "v2", GetRequestId());
                }

                if (policy.AuditLevel >= AuditLevel.Full)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptDetail scriptLength={0} languageMode={1} rid={2}",
                        script.Length, languageMode, GetRequestId());
                }
            }

            // Stream capture bootstrap: injected after policy validation so the
            // policy scanner never sees the Write-* redefinitions. Only fires
            // when the client signals it via captureStreams=true, which the
            // Invoke-RemoteScript module sets when -Verbose or -Debug is used.
            var captureStreams = string.Equals(context.Request.QueryString[ParamCaptureStreams], "true", StringComparison.OrdinalIgnoreCase);
            if (captureStreams && isInlineScript && !string.IsNullOrEmpty(script))
            {
                script = StreamCaptureBootstrap + script;
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
                context.Response.Headers["X-SPE-LanguageMode"] = languageMode.ToString();
                session.ActiveRemotingPolicy = policy;

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

                    // Skip anything IIS populates in ServerVariables (HTTP_*,
                    // REMOTE_*, SERVER_*, CERT_*, ...) - the old code carried
                    // a 20-prefix StartsWith chain tested against every key.
                    // The prefix list tracked ServerVariables anyway, so a
                    // HashSet of actual ServerVariable names is both faster
                    // and drift-proof: if IIS adds a new prefix, we skip it
                    // without a code change.
                    var serverVarKeys = new HashSet<string>(
                        context.Request.ServerVariables.AllKeys ?? Array.Empty<string>(),
                        StringComparer.OrdinalIgnoreCase);

                    foreach (var param in context.Request.Params.AllKeys)
                    {
                        if (string.IsNullOrEmpty(param)) continue;
                        if (serverVarKeys.Contains(param)) continue;

                        var paramValue = context.Request.Params[param];
                        if (string.IsNullOrEmpty(paramValue)) continue;

                        if (session.GetVariable(param) == null)
                        {
                            session.SetVariable(param, paramValue);
                        }
                    }

                    session.SetVariable("requestStreams", streams);
                    session.SetVariable("scriptArguments", scriptArguments);

                    if (policy.AuditLevel >= AuditLevel.Full)
                    {
                        PowerShellLog.Audit("[Remoting] action=requestDetail paramCount={0} rid={1}",
                            scriptArguments.Count, GetRequestId());
                    }

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

                if (policy.AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptCompleted user={0} ip={1} session={2} scriptHash={3} hasErrors={4} clientSession={5} rid={6}",
                        user, ip, session.ID, scriptHash.Value, session.Output.HasErrors, clientSession, GetRequestId());
                }

                if (policy.AuditLevel >= AuditLevel.Full)
                {
                    PowerShellLog.Audit("[Remoting] action=responseDetail outputFormat={0} statusCode={1} rid={2}",
                        outputFormat, context.Response.StatusCode, GetRequestId());
                }
            }
            catch (Exception ex)
            {
                if (policy.AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptFailed user={0} ip={1} session={2} scriptHash={3} error={4} clientSession={5} rid={6}",
                        user, ip, session.ID, scriptHash.Value, ex.GetType().Name, clientSession, GetRequestId());
                }
                PowerShellLog.Error("[Remoting] action=scriptFailed error=scriptExecution", ex);
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