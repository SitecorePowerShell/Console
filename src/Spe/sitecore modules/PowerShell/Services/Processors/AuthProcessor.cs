using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using System.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Security;
using Spe.Core.Diagnostics;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class AuthProcessor
    {
        internal const string ParamUser = "user";
        internal const string ParamPassword = "password";

        // Process-wide flags so a misconfigured deployment surfaces the OAuth
        // dispatch failure once at Warn level (visible without enabling Debug)
        // instead of silently rejecting every request. Reset on AppDomain recycle.
        private static int _oauthUnregisteredWarningEmitted;
        private static int _oauthIssuerMissingWarningEmitted;
        private static int _oauthNotMatchedWarningEmitted;

        public static bool AuthenticateRequest(HttpContext context, string serviceName, out AccountIdentity identity, out bool isAuthenticated)
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
                if (!WebServiceSettings.AllowQueryStringCredentials)
                {
                    PowerShellLog.Warn($"[Remoting] action=credentialRejected service={serviceName} ip={RemotingRequestContext.GetIp(request)} user={LogSanitizer.SanitizeValue(username)} reason=queryStringCredentials");
                    // Sanitized form mirrors RejectAuthenticationMethod verbatim so the
                    // response surface is indistinguishable from a normal auth failure -
                    // the gate's existence is not leaked to unauthenticated callers.
                    // Verbose form adds a non-disclosing hint for dev operators; the
                    // setting name stays in the audit log only.
                    var errorMessage = WebServiceSettings.DetailedErrors
                        ? $"Unauthorized request to the {serviceName} service. Use the Authorization header."
                        : $"Unauthorized request to the {serviceName} service.";
                    ErrorResponseWriter.SetErrorResponse(context, 401, errorMessage, true);
                    return false;
                }

                PowerShellLog.Warn($"[Remoting] action=deprecatedAuth service={serviceName} ip={RemotingRequestContext.GetIp(request)} user={LogSanitizer.SanitizeValue(username)} reason=queryStringCredentials");
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
                    var ip = RemotingRequestContext.GetIp(request);
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
                                        PowerShellLog.Warn($"[Remoting] action=coldStart ip={ip} rid={RemotingRequestContext.GetRequestId()}");
                                        context.Response.AddHeader("Retry-After", "2");
                                        ErrorResponseWriter.SetErrorResponse(context, 503, "Service is starting. Please retry.");
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
                                            PowerShellLog.Debug($"[Remoting] action=clientSignatureFailed kid={LogSanitizer.SanitizeValue(kid)} remotingClient={matchedClient.Id:D}");
                                            authFailureReason = RemotingClientProvider.AuthFailureReasonInvalid;
                                            matchedClient = null;
                                        }
                                    }
                                    else
                                    {
                                        authFailureReason = RemotingClientProvider.GetAuthFailureReason(kid)
                                            ?? RemotingClientProvider.AuthFailureReasonInvalid;
                                        PowerShellLog.Debug($"[Remoting] action=clientNotFound kid={LogSanitizer.SanitizeValue(kid)} reason={authFailureReason}");
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
                            string peekedIssuer = null;
                            var hasIssuer = OAuthBearerTokenAuthenticationProvider.TryPeekIssuer(token, out peekedIssuer);
                            if (hasIssuer)
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
                                // Diagnose dispatch failure for the operator. Each root cause
                                // logs once at Warn level (visible at default log level), then
                                // suppresses follow-on noise. AppDomain recycle resets the gate.
                                var sanitizedAlg = LogSanitizer.SanitizeValue(alg);
                                var sanitizedIss = LogSanitizer.SanitizeValue(peekedIssuer ?? "missing");
                                if (!hasIssuer)
                                {
                                    if (Interlocked.CompareExchange(ref _oauthIssuerMissingWarningEmitted, 1, 0) == 0)
                                    {
                                        PowerShellLog.Warn($"[Remoting] action=oauthDispatchFailed reason=tokenIssuerMissing alg={sanitizedAlg} hint=Token has no iss claim or payload is malformed; OAuth routing requires iss.");
                                    }
                                    else
                                    {
                                        PowerShellLog.Debug($"[Remoting] action=oauthDispatchFailed reason=tokenIssuerMissing alg={sanitizedAlg}");
                                    }
                                }
                                else
                                {
                                    var oauthCount = providers.Count(p => p is OAuthBearerTokenAuthenticationProvider);
                                    if (oauthCount == 0)
                                    {
                                        if (Interlocked.CompareExchange(ref _oauthUnregisteredWarningEmitted, 1, 0) == 0)
                                        {
                                            PowerShellLog.Warn($"[Remoting] action=oauthDispatchFailed reason=oauthProviderUnregistered alg={sanitizedAlg} iss={sanitizedIss} hint=Drop the .example suffix from Spe.OAuthBearer.config and configure jwksUri/allowedIssuers/allowedAudiences for your IdP.");
                                        }
                                        else
                                        {
                                            PowerShellLog.Debug($"[Remoting] action=oauthDispatchFailed reason=oauthProviderUnregistered alg={sanitizedAlg} iss={sanitizedIss}");
                                        }
                                    }
                                    else
                                    {
                                        if (Interlocked.CompareExchange(ref _oauthNotMatchedWarningEmitted, 1, 0) == 0)
                                        {
                                            PowerShellLog.Warn($"[Remoting] action=oauthDispatchFailed reason=oauthProviderNotMatched alg={sanitizedAlg} iss={sanitizedIss} oauthProviderCount={oauthCount} hint=Verify your <oauthBearer> entries' allowedIssuers and allowedAlgorithms include this token's iss and alg.");
                                        }
                                        else
                                        {
                                            PowerShellLog.Debug($"[Remoting] action=oauthDispatchFailed reason=oauthProviderNotMatched alg={sanitizedAlg} iss={sanitizedIss} oauthProviderCount={oauthCount}");
                                        }
                                    }
                                }
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
                                    PowerShellLog.Audit("[Remoting] action=clientDenied reason=noImpersonateUser remotingClient={0} remotingClientKid={1} ip={2} rid={3}",
                                        RemotingAuditFormat.FormatRemotingClientId(matchedClient), RemotingAuditFormat.FormatRemotingClientKid(matchedClient), ip, RemotingRequestContext.GetRequestId());
                                    ErrorResponseWriter.SetErrorResponse(context, 403, "This Remoting Client requires an Impersonated User. Configure the field on the client item.");
                                    return false;
                                }

                                username = matchedClient.ImpersonateUser;
                            }

                            // Throttle check
                            if (matchedClient != null)
                            {
                                var throttle = RemotingClientProvider.CheckThrottle(matchedClient);
                                HttpResponseHelpers.AddThrottleHeaders(context.Response, throttle);

                                if (!throttle.Allowed)
                                {
                                    var throttlePolicy = RemotingPolicyManager.ResolvePolicy(matchedClient.Policy);
                                    if (string.Equals(throttle.Action, "Bypass", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (throttlePolicy.AuditLevel >= AuditLevel.Standard)
                                        {
                                            PowerShellLog.Audit("[Remoting] action=throttleBypassed remotingClient={0} remotingClientKid={1} ip={2} rid={3}",
                                                RemotingAuditFormat.FormatRemotingClientId(matchedClient), RemotingAuditFormat.FormatRemotingClientKid(matchedClient), ip, RemotingRequestContext.GetRequestId());
                                        }
                                    }
                                    else
                                    {
                                        if (throttlePolicy.AuditLevel >= AuditLevel.Violations)
                                        {
                                            PowerShellLog.Audit("[Remoting] action=throttled remotingClient={0} remotingClientKid={1} ip={2} rid={3}",
                                                RemotingAuditFormat.FormatRemotingClientId(matchedClient), RemotingAuditFormat.FormatRemotingClientKid(matchedClient), ip, RemotingRequestContext.GetRequestId());
                                        }
                                        ErrorResponseWriter.SetErrorResponse(context, 429, "Rate limit exceeded.");
                                        return false;
                                    }
                                }
                            }

                            authenticationManager.SwitchToUser(username, true);
                            PowerShellLog.Debug($"[Remoting] action=bearerAuthSuccess authKind={authKind ?? "none"} user={LogSanitizer.SanitizeValue(username)} ip={ip} clientSession={tokenResult?.ClientSessionId ?? "none"} remotingClient={RemotingAuditFormat.FormatRemotingClientId(matchedClient)} remotingClientKid={RemotingAuditFormat.FormatRemotingClientKid(matchedClient)} clientId={LogSanitizer.SanitizeValue(tokenResult?.ClientId ?? "none")}");
                            if (tokenResult != null)
                            {
                                HttpContext.Current.Items[RemotingHttpContextKeys.TokenResult] = tokenResult;
                            }
                            if (matchedClient != null)
                            {
                                HttpContext.Current.Items[RemotingHttpContextKeys.RemotingClient] = matchedClient;
                            }
                            // Stash so the top-level action=authenticated audit line
                            // (which fires after AuthenticateRequest returns) can render
                            // authKind= without re-deriving it from headers/state.
                            HttpContext.Current.Items[RemotingHttpContextKeys.AuthKind] = authKind;
                        }
                        else
                        {
                            PowerShellLog.Audit("[Remoting] action=bearerAuthFailed authKind={0} user={1} ip={2} remotingClient={3} remotingClientKid={4} clientId={5} rid={6}",
                                authKind ?? "none", LogSanitizer.SanitizeValue(username ?? "unknown"), ip,
                                RemotingAuditFormat.FormatRemotingClientId(matchedClient), RemotingAuditFormat.FormatRemotingClientKid(matchedClient),
                                LogSanitizer.SanitizeValue(tokenResult?.ClientId ?? "none"), RemotingRequestContext.GetRequestId());
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
                        PowerShellLog.Audit("[Remoting] action=bearerAuthError user={0} ip={1} error={2} rid={3}", username ?? "unknown", ip, LogSanitizer.SanitizeValue(ex.Message), RemotingRequestContext.GetRequestId());
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

            try
            {
                identity = new AccountIdentity(authUserName);
            }
            catch (ArgumentException ex)
            {
                // AccountIdentity rejects malformed usernames with an
                // ArgumentException whose message echoes the raw input.
                // Letting it propagate routes the unsanitized name through
                // Sitecore's default Application error logger, bypassing
                // PowerShellLog + LogSanitizer. Catch here so the sanitized
                // path in RejectAuthenticationMethod handles the audit.
                RejectAuthenticationMethod(context, serviceName, authUserName, ex);
                return false;
            }
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

        public static bool CheckServiceEnabled(HttpContext context, string serviceName)
        {
            var isEnabled = WebServiceSettings.IsEnabled(serviceName);
            if (isEnabled) return true;

            var ip = RemotingRequestContext.GetIp(context.Request);
            var errorMessage = $"The {serviceName} service is disabled.";
            ErrorResponseWriter.SetErrorResponse(context, 403, errorMessage);
            PowerShellLog.Warn($"[Remoting] action=serviceDisabled service={serviceName} ip={ip}");

            return false;
        }

        private static bool CheckServiceAuthentication(HttpContext context, string serviceName, bool isAuthenticated)
        {
            if (isAuthenticated) return true;

            var ip = RemotingRequestContext.GetIp(context.Request);
            var errorMessage = $"The {serviceName} service requires authentication.";
            ErrorResponseWriter.SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Warn($"[Remoting] action=authRequired service={serviceName} ip={ip}");

            return false;
        }

        public static void RejectAuthenticationMethod(HttpContext context, string serviceName, string username = null, Exception ex = null)
        {
            var ip = RemotingRequestContext.GetIp(context.Request);
            var errorMessage = $"Unauthorized request to the {serviceName} service.";
            ErrorResponseWriter.SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Audit($"[Remoting] action=authRejected service={serviceName} ip={ip} user={LogSanitizer.SanitizeValue(username ?? "unknown")} rid={RemotingRequestContext.GetRequestId()}");

            if (ex != null)
            {
                // Sanitize before StatusDescription assignment: HttpResponse
                // throws HttpException on CR/LF in the reason phrase, and the
                // AccountIdentity ArgumentException message contains literal
                // newlines. Sanitized form also prevents response-splitting
                // via attacker-controlled exception text.
                var safeMessage = LogSanitizer.SanitizeValue(ex.Message);
                context.Response.StatusDescription += $" {safeMessage}";
                PowerShellLog.Error($"[Remoting] action=authRejected error={safeMessage}");
            }
        }

        private static bool CheckIsUserAuthorized(HttpContext context, string authUserName, string serviceName)
        {
            var isAuthorized = ServiceAuthorizationManager.IsUserAuthorized(serviceName, authUserName);
            if (isAuthorized) return true;

            var ip = RemotingRequestContext.GetIp(context.Request);
            var errorMessage = $"The specified user is not authorized for the {serviceName} service.";
            ErrorResponseWriter.SetErrorResponse(context, 401, errorMessage, true);
            PowerShellLog.Audit($"[Remoting] action=userUnauthorized service={serviceName} ip={ip} user={LogSanitizer.SanitizeValue(authUserName)} rid={RemotingRequestContext.GetRequestId()}");

            return false;
        }
    }
}
