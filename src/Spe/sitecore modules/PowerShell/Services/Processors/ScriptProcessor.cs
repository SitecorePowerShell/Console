using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Sites;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class ScriptProcessor
    {
        // PowerShell snippets compiled into the response pipeline. Kept as
        // private const so the JIT can fold them into the call site, and kept
        // here (not in a shared helper) so the only consumer also owns the
        // strings - no other processor in the assembly should depend on these.

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
            "correlationId = $rid; " +
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

        // Sanitized counterpart used when Spe.Remoting.DetailedErrors=false.
        // Keeps the PS-provided category and FullyQualifiedErrorId (callers can
        // produce these themselves via their own script - leaking them tells
        // the caller nothing they didn't already know) plus the per-request
        // correlationId so operators can match a complaint to an audit-log
        // entry. The message is a fixed template that quotes the
        // correlationId so the SPE client's Write-JsonErrors path (which
        // reads exceptionMessage / message and fullyQualifiedErrorId) still
        // surfaces something meaningful via Write-Error. Drops everything
        // else: stack traces, exception types, invocation info, position
        // messages.
        private const string SanitizedStructuredErrorScript =
            "@{ output = @($outObjects); errors = @($errorObjects | ForEach-Object { " +
            "@{ " +
            "correlationId = $rid; " +
            "errorCategory = \"$($_.CategoryInfo.Category)\"; " +
            "fullyQualifiedErrorId = $_.FullyQualifiedErrorId; " +
            "message = \"Script error. See SPE log id=$rid.\" " +
            "} }) } | ConvertTo-Json -Depth 3 -Compress";

        private const string FlatErrorScript =
            "@{ output = @($outObjects); errors = @($errorObjects | ForEach-Object { $_.ToString() }) } | ConvertTo-Json -Depth 3 -Compress";

        public static void ProcessScript(HttpContext context, HttpRequest request, string serviceName, string outputFormat, string sessionId, bool persistentSession, bool useStructuredErrors = false)
        {
            if (request?.InputStream == null)
            {
                // Empty-body POST hits this; without the audit the call shows
                // only requestReceived + authenticated and looks identical to
                // a silently-suppressed scriptStarting.
                if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptRejected reason=missingBody sessionId={0} ip={1} rid={2}",
                        sessionId ?? "<none>", RemotingRequestContext.GetIp(context.Request), RemotingRequestContext.GetRequestId(context));
                }
                return;
            }

            string script = null;
            string cliXmlArgs = null;
            string requestBody;
            try
            {
                requestBody = ReadRequestBody(request);
            }
            catch (RequestBodyTooLargeException)
            {
                if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptRejected reason=bodyTooLarge cap={0} sessionId={1} ip={2} rid={3}",
                        WebServiceSettings.MaxScriptBodySize, sessionId ?? "<none>", RemotingRequestContext.GetIp(context.Request), RemotingRequestContext.GetRequestId(context));
                }
                var sizeMsg = WebServiceSettings.DetailedErrors
                    ? $"Request body exceeds the configured cap of {WebServiceSettings.MaxScriptBodySize} bytes."
                    : "The request body could not be processed.";
                ErrorResponseWriter.SetErrorResponse(context, 413, sizeMsg);
                return;
            }
            catch (InvalidDataException)
            {
                if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptRejected reason=malformedBody sessionId={0} ip={1} rid={2}",
                        sessionId ?? "<none>", RemotingRequestContext.GetIp(context.Request), RemotingRequestContext.GetRequestId(context));
                }
                var formatMsg = WebServiceSettings.DetailedErrors
                    ? "The request body could not be decompressed."
                    : "The request body could not be processed.";
                ErrorResponseWriter.SetErrorResponse(context, 400, formatMsg);
                return;
            }
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

        public static void ProcessScript(HttpContext context, Item scriptItem, string serviceName)
        {
            if (!scriptItem.IsPowerShellScript() || scriptItem?.Fields[Templates.Script.Fields.ScriptBody] == null)
            {
                ErrorResponseWriter.SetErrorResponse(context, 404, "The specified script is invalid.");
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
                if (RemotingRequestContext.GetActivePolicy().AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptRejected reason=emptyScript sessionId={0} ip={1} rid={2}",
                        sessionId ?? "<none>", RemotingRequestContext.GetIp(context.Request), RemotingRequestContext.GetRequestId(context));
                }
                ErrorResponseWriter.SetErrorResponse(context, 400, "The specified script is invalid.");
                return;
            }

            var user = Sitecore.Context.User?.Name ?? "unknown";
            var ip = RemotingRequestContext.GetIp(context.Request);
            // Lazy: the hash is only consumed inside AuditLevel >= Standard
            // guards. Skip the SHA256 + hex conversion entirely when the
            // active policy runs at None or Violations.
            var scriptHash = new Lazy<string>(() => RemotingRequestContext.ComputeScriptHash(script));
            var tokenResult = HttpContext.Current?.Items[RemotingHttpContextKeys.TokenResult] as TokenValidationResult;
            var clientSession = tokenResult?.ClientSessionId ?? "none";

            // scriptStarting fires here, before GetSession, so the audit captures
            // the script-arrived event without waiting on runspace warmup.
            // GetSession can take seconds on a cold runspace pool (snap-in load,
            // InitialSessionState build, alias-setup); deferring this audit until
            // after that left the operator looking at request+auth lines with no
            // follow-up during the warmup window. The URL sessionId is the same
            // value that ends up in session.ID for the standard Invoke-RemoteScript
            // -Session path; for the rare empty-sessionId case the audit shows
            // "<auto>" and the rest of the request continues to use a server-
            // generated GUID internally.
            var auditSessionId = string.IsNullOrEmpty(sessionId) ? "<auto>" : sessionId;
            // sessionMode mirrors ScriptSessionManager.GetSession's autoDispose
            // predicate: an empty URL sessionId becomes a one-shot session,
            // anything else is reused for the lifetime of the cache entry.
            var sessionMode = string.IsNullOrEmpty(sessionId) ? "transient" : "persistent";
            var activePolicy = RemotingRequestContext.GetActivePolicy();
            if (activePolicy.AuditLevel >= AuditLevel.Standard)
            {
                PowerShellLog.Audit("[Remoting] action=scriptStarting user={0} ip={1} sessionId={2} sessionMode={3} scriptHash={4} clientSession={5} rid={6}",
                    user, ip, auditSessionId, sessionMode, scriptHash.Value, clientSession, RemotingRequestContext.GetRequestId());
            }
            else
            {
                // Diagnostic for "no script-execute audit lines despite policy=Standard"
                // confusion. Lands in the regular log (log.*.txt) so it does not pollute
                // the audit stream that operators grep for security events. Tells the
                // operator exactly which policy resolved and what AuditLevel suppressed
                // the audit chain.
                PowerShellLog.Info($"[Remoting] action=auditSuppressed reason=auditLevelBelowStandard policy={activePolicy.Name} auditLevel={activePolicy.AuditLevel} sessionId={auditSessionId} rid={RemotingRequestContext.GetRequestId()}");
            }

            var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            // Session ownership: first caller claims the session; subsequent
            // calls with a different identity are rejected. Prevents a second
            // authenticated principal (e.g. a different Remoting Client) from
            // attaching to a session id they did not create. Emits its own
            // sessionOwnershipMismatch audit when rejecting, so the gap
            // between scriptStarting and (no scriptCompleted) is explained.
            if (!SessionOwnershipGuard.TryEnforce(context, session, ip))
            {
                return;
            }

            // Language mode defaults to FullLanguage; policy may restrict it below
            var languageMode = System.Management.Automation.PSLanguageMode.FullLanguage;

            // Resolve remoting policy (Remoting Client > unrestricted for config-based shared-secret)
            var client = HttpContext.Current?.Items[RemotingHttpContextKeys.RemotingClient] as RemotingClient;

            RemotingPolicy policy;
            if (client != null)
            {
                if (!client.HasPolicy)
                {
                    PowerShellLog.Audit(
                        "[Remoting] action=clientDenied reason=noPolicyAssigned remotingClient={0} remotingClientKid={1} user={2} ip={3} rid={4}",
                        RemotingAuditFormat.FormatRemotingClientId(client), RemotingAuditFormat.FormatRemotingClientKid(client), user, ip, RemotingRequestContext.GetRequestId());
                    ErrorResponseWriter.SetErrorResponse(context, 403, "The request is not authorized. A remoting policy is required.");
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
                                user, ip, policy.Name, policyBlockedCommand, clientSession, RemotingRequestContext.GetRequestId());
                        }
                        // X-Spe-Restriction is the category and stays visible
                        // unconditionally - the client (Invoke-RemoteScript)
                        // uses it to pick a friendly error path. The specific
                        // command and policy name leak details an
                        // unauthenticated probe could use to enumerate the
                        // allowlist; gate them on Spe.Remoting.DetailedErrors
                        // so the audit log keeps the truth and ops can still
                        // turn the headers back on for dev troubleshooting.
                        context.Response.Headers["X-Spe-Restriction"] = "policy-blocked";
                        if (WebServiceSettings.DetailedErrors)
                        {
                            context.Response.Headers["X-Spe-BlockedCommand"] = policyBlockedCommand;
                            context.Response.Headers["X-Spe-Policy"] = policy.Name;
                            ErrorResponseWriter.SetErrorResponse(context, 403, $"Script blocked by remoting policy '{policy.Name}': {policyBlockedCommand}");
                        }
                        else
                        {
                            ErrorResponseWriter.SetErrorResponse(context, 403, "Script blocked by remoting policy.");
                        }
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
                                scriptItemId, policy.Name, user, ip, RemotingRequestContext.GetRequestId());
                        }
                        ErrorResponseWriter.SetErrorResponse(context, 403, "The requested script is not approved under this policy.");
                        return;
                    }

                    if (policy.AuditLevel >= AuditLevel.Standard)
                    {
                        PowerShellLog.Audit(
                            "[Approval] action=scriptApproved script={0} policy={1} rid={2}",
                            scriptItemId, policy.Name, RemotingRequestContext.GetRequestId());
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
                        serviceName, policy.Name, scriptHash.Value, clientSession, isInlineScript ? "inline" : "v2", RemotingRequestContext.GetRequestId());
                }

                if (policy.AuditLevel >= AuditLevel.Full)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptDetail scriptLength={0} languageMode={1} rid={2}",
                        script.Length, languageMode, RemotingRequestContext.GetRequestId());
                }
            }

            // Stream capture bootstrap: injected after policy validation so the
            // policy scanner never sees the Write-* redefinitions. Only fires
            // when the client signals it via captureStreams=true, which the
            // Invoke-RemoteScript module sets when -Verbose or -Debug is used.
            var captureStreams = string.Equals(context.Request.QueryString["captureStreams"], "true", StringComparison.OrdinalIgnoreCase);
            if (captureStreams && isInlineScript && !string.IsNullOrEmpty(script))
            {
                script = StreamCaptureBootstrap + script;
            }

            // Wall-clock timing for the execute-and-serialize block. Started
            // here (rather than at scriptStarting) so the figure measures only
            // what the script and its response shaping consumed - session
            // warmup, ownership checks, and policy validation are excluded.
            var executionStopwatch = Stopwatch.StartNew();
            try
            {
                if (Sitecore.Context.Database != null)
                {
                    var item = Sitecore.Context.Database.GetRootItem();
                    if (item != null)
                        session.SetItemLocationContext(item);
                }

                context.Response.ContentType = "text/plain";
                context.Response.Headers["X-Spe-LanguageMode"] = languageMode.ToString();
                RemotingPolicyManager.ApplyPolicyToSession(session, policy);

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
                            scriptArguments.Count, RemotingRequestContext.GetRequestId());
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
                    var site = SiteContextFactory.GetSiteContext(requestUri.Host, Sitecore.Context.Request.FilePath,
                        requestUri.Port);
                    Sitecore.Context.SetActiveSite(site.Name);

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
                            session.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results", true, true);

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
                        session.SetVariable("rid", RemotingRequestContext.GetRequestId());

                        session.Output.Clear();
                        // Gate the verbose structured form behind Spe.Remoting.DetailedErrors.
                        // FlatErrorScript path is unchanged - it returns just the message
                        // string, which is the legacy contract. Verbose / sanitized split
                        // only applies when the caller explicitly asked for structured.
                        var structuredScript = WebServiceSettings.DetailedErrors
                            ? StructuredErrorScript
                            : SanitizedStructuredErrorScript;
                        session.ExecuteScriptPart(useStructuredErrors ? structuredScript : FlatErrorScript, true, true);

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
                            session.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results", true, true);

                            foreach (var outputBuffer in session.Output)
                            {
                                response.Write(outputBuffer.Text);
                            }
                        }
                    }
                }

                // Two error signals trigger 424:
                //   * Output.HasErrors - flipped when something rendered
                //     through the host UI's WriteErrorLine. Default-action
                //     errors and Write-Error reach the host this way.
                //   * LastInvocationHadErrors - flipped only when a
                //     pipeline-output object is an ErrorRecord (Out-Default
                //     re-emitting a binding failure as output). The script
                //     can't have handled that case.
                // We deliberately do NOT read session.LastErrors / Streams.Error
                // here: Sitecore's PSProvider writes errors that bypass
                // -ErrorAction Ignore/SilentlyContinue/try-catch, so counting
                // those gave every "if exists" test setup a spurious 424.
                // Uncaught terminating exceptions are handled by the
                // catch (Exception) block below and still set 424.
                var hasErrors = session.Output.HasErrors
                    || session.LastInvocationHadErrors;

                if (hasErrors)
                {
                    context.Response.StatusCode = 424;
                    context.Response.StatusDescription = "Method Failure";
                }

                // Stop unconditionally so the response header carries the
                // figure regardless of audit level. ASP.NET buffers the
                // response by default so headers stay mutable until the
                // request ends.
                executionStopwatch.Stop();
                context.Response.Headers["X-Spe-Duration-Ms"] = executionStopwatch.ElapsedMilliseconds.ToString();

                if (policy.AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptCompleted user={0} ip={1} sessionId={2} scriptHash={3} hasErrors={4} durationMs={5} clientSession={6} rid={7}",
                        user, ip, session.ID, scriptHash.Value, hasErrors, executionStopwatch.ElapsedMilliseconds, clientSession, RemotingRequestContext.GetRequestId());
                }

                if (policy.AuditLevel >= AuditLevel.Full)
                {
                    PowerShellLog.Audit("[Remoting] action=responseDetail outputFormat={0} statusCode={1} rid={2}",
                        outputFormat, context.Response.StatusCode, RemotingRequestContext.GetRequestId());
                }
            }
            catch (Exception ex)
            {
                executionStopwatch.Stop();
                context.Response.Headers["X-Spe-Duration-Ms"] = executionStopwatch.ElapsedMilliseconds.ToString();

                if (policy.AuditLevel >= AuditLevel.Standard)
                {
                    PowerShellLog.Audit("[Remoting] action=scriptFailed user={0} ip={1} sessionId={2} scriptHash={3} error={4} durationMs={5} clientSession={6} rid={7}",
                        user, ip, session.ID, scriptHash.Value, ex.GetType().Name, executionStopwatch.ElapsedMilliseconds, clientSession, RemotingRequestContext.GetRequestId());
                }
                PowerShellLog.Error("[Remoting] action=scriptFailed error=scriptExecution", ex);
                context.Response.StatusCode = 424;
                context.Response.StatusDescription = "Method Failure";

                if (outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.ContentType = "application/json";
                    if (useStructuredErrors)
                    {
                        var rid = RemotingRequestContext.GetRequestId();
                        var errorEntry = new Hashtable
                        {
                            ["correlationId"] = rid,
                            ["errorCategory"] = "NotSpecified",
                            ["fullyQualifiedErrorId"] = "Spe.Remoting.ScriptExecutionFailed",
                            ["message"] = $"Script execution failed. See SPE log id={rid}."
                        };
                        if (WebServiceSettings.DetailedErrors)
                        {
                            errorEntry["message"] = ex.Message;
                            errorEntry["exceptionType"] = ex.GetType().FullName;
                            errorEntry["exceptionMessage"] = ex.Message;
                            errorEntry["scriptStackTrace"] = ex.StackTrace;
                        }
                        var errorBody = new Hashtable
                        {
                            ["output"] = new object[0],
                            ["errors"] = new[] { errorEntry }
                        };
                        context.Response.Write(new JavaScriptSerializer().Serialize(errorBody));
                    }
                    else
                    {
                        // Flat form: the message itself is the only field.
                        // When DetailedErrors=false, return the rid so the
                        // caller can quote it to an admin instead of an
                        // ex.Message that may carry server-internal detail.
                        var errorText = WebServiceSettings.DetailedErrors
                            ? ex.Message
                            : $"Script execution failed. correlationId={RemotingRequestContext.GetRequestId()}";
                        var errorBody = new Hashtable
                        {
                            ["output"] = new object[0],
                            ["errors"] = new[] { errorText }
                        };
                        context.Response.Write(new JavaScriptSerializer().Serialize(errorBody));
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

        private static string ReadRequestBody(HttpRequest request)
        {
            // Both branches stream through LimitedReadStream so the cap
            // applies before allocation: the gzip path bounds the decoded
            // bytes (compressed bytes are bounded by ASP.NET maxRequestLength),
            // and the plaintext path bounds the read of request.InputStream
            // itself, fail-fasting before StreamReader materialises the body
            // as a string. leaveOpen on GZipStream keeps request.InputStream
            // alive for ASP.NET to dispose.
            var cap = WebServiceSettings.MaxScriptBodySize;
            var encoding = request.Headers["Content-Encoding"];
            var isGzip = !string.IsNullOrEmpty(encoding) &&
                         encoding.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isGzip)
            {
                using (var gz = new GZipStream(request.InputStream, CompressionMode.Decompress, leaveOpen: true))
                using (var capped = new LimitedReadStream(gz, cap))
                using (var reader = new StreamReader(capped, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }

            using (var capped = new LimitedReadStream(request.InputStream, cap))
            using (var reader = new StreamReader(capped, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
