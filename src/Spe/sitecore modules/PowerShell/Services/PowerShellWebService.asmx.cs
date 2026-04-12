using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Services;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Security.Accounts;
using Sitecore.StringExtensions;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Debugging;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;
using LicenseManager = Sitecore.SecurityModel.License.LicenseManager;
using PSCustomObject = System.Management.Automation.PSCustomObject;
using PSObject = System.Management.Automation.PSObject;

namespace Spe.sitecore_modules.PowerShell.Services
{
    /// <summary>
    ///     Summary description for PowerShellWebService
    ///     The service is used by Terminal/Console app and the Sitecore Rocks Visual Studio console
    ///     for all of their operations. It's also used by ISE for code completions.
    /// </summary>
    [WebService(Namespace = "http://sitecorepowershellextensions/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    [ScriptService]
    public class PowerShellWebService : WebService
    {
        private const string StatusComplete = "complete";
        private const string StatusPartial = "partial";
        private const string StatusWorking = "working";
        private const string StatusError = "error";
        private const string StatusElevationRequired = "unauthorized";
        private static readonly string[] ImportantProperties = {"Name", "Title"};

        private static bool IsLoggedInUserAuthorized =>
            Sitecore.Context.IsLoggedIn &&
            ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceClient, Sitecore.Context.User?.Name);


        [WebMethod(EnableSession = true)]
        public bool LoginUser(string userName, string password)
        {
            if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceClient, userName))
            {
                return false;
            }

            if (!userName.Contains("\\"))
            {
                userName = "sitecore\\" + userName;
            }

            var authenticationManager = TypeResolver.ResolveFromCache<IAuthenticationManager>();
            if (Sitecore.Context.IsLoggedIn)
            {
                if (Sitecore.Context.User.Name.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    return true;
                authenticationManager.Logout();
            }
            
            if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
                throw new AccessDeniedException("A required license is missing");

            Assert.IsTrue(authenticationManager.ValidateUser(userName, password), "Unknown username or password.");
            var user = Sitecore.Security.Accounts.User.FromName(userName, true);
            UserSwitcher.Enter(user);
            return true;
        }

        [WebMethod(EnableSession = true)]
        public object ExecuteCommand(string guid, string command, string stringFormat)
        {
            var serializer = new JavaScriptSerializer();
            var output = new StringBuilder();

            if (!IsLoggedInUserAuthorized ||
                !SessionElevationManager.IsSessionTokenElevated(ApplicationNames.Console))
            {
                return serializer.Serialize(
                    new
                    {
                        status = StatusElevationRequired,
                        result =
                        "You need to be authenticated, elevated and have sufficient privileges to use the PowerShell console. Please (re)login to Sitecore.",
                        prompt = "PS >",
                        background = OutputLine.ProcessHtmlColor(ConsoleColor.DarkBlue)
                    });
            }

            PowerShellLog.Audit($"[Console] action=scriptExecuting session={guid} user={Sitecore.Context.User?.Name}");

            var session = GetScriptSession(guid);
            session.Interactive = true;
            session.SetItemContextFromLocation();
            try
            {
                var handle = ID.NewID.ToString();
                var jobOptions = TypeResolver.Resolve<IJobOptions>(new object[]{GetJobId(guid, handle), "PowerShell", "shell", this, nameof(RunJob),
                    new object[] {session, command}});
                jobOptions.ContextUser = Sitecore.Context.User;
                jobOptions.EnableSecurity = true;
                jobOptions.ClientLanguage = Sitecore.Context.ContentLanguage;
                jobOptions.AfterLife = new TimeSpan(0, 0, 20);


                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                jobManager.StartJob(jobOptions);

                Thread.Sleep(WebServiceSettings.CommandWaitMillis);
                return PollCommandOutput(guid, handle, stringFormat);
            }
            catch (Exception ex)
            {
                return
                    serializer.Serialize(
                        new Result
                        {
                            status = StatusError,
                            result =
                                output +
                                ScriptSession.GetExceptionString(ex, ScriptSession.ExceptionStringFormat.Console) +
                                "\r\n" +
                                "\r\n[[;#f00;#000]Uh oh, looks like the command you ran is invalid or something else went wrong. Is it something we should know about?]\r\n" +
                                "[[;#f00;#000]Please submit a support ticket here https://git.io/spe with error details, screenshots, and anything else that might help.]\r\n\r\n" +
                                "[[;#f00;#000]We also have a user guide here https://doc.sitecorepowershell.com/.]\r\n\r\n",
                            prompt = $"PS {session.CurrentLocation}>",
                            background = OutputLine.ProcessHtmlColor(session.PrivateData.BackgroundColor),
                            color = OutputLine.ProcessHtmlColor(session.PrivateData.ForegroundColor)
                        });
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object KeepAlive(string guid)
        {
            if (!IsLoggedInUserAuthorized)
            {
                return string.Empty;
            }
            var sessionExists = ScriptSessionManager.SessionExists(guid);
            return sessionExists ? "alive" : "session-not-found";
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object GetSessionVariables(string guid)
        {
            if (!IsLoggedInUserAuthorized)
            {
                return string.Empty;
            }

            var serializer = new JavaScriptSerializer();
            if (!ScriptSessionManager.SessionExists(guid))
            {
                return serializer.Serialize(new { status = "no-session", variables = new object[0] });
            }

            var session = ScriptSessionManager.GetSession(guid);
            var vars = session.GetUserVariables()
                .Select(v => new { name = v.Name, type = v.TypeName, value = v.Preview, category = v.Category, expandable = v.Expandable })
                .ToArray();
            return serializer.Serialize(new { status = "ok", variables = vars });
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object GetVariableValue(string guid, string variableName)
        {
            if (!IsLoggedInUserAuthorized)
            {
                return string.Empty;
            }

            var sessionExists = ScriptSessionManager.SessionExists(guid);
            if (!sessionExists)
            {
                return "<div class='undefinedVariableType'>Session not found</div>" +
                       "<div class='variableLine'>A script needs to be executed in the session<br/>before the variable value can be inspected.</div>";
            }

            var session = ScriptSessionManager.GetSession(guid);
            try
            {
                variableName = variableName.TrimStart('$');
                var debugVariable = session.GetDebugVariable(variableName);
                if (debugVariable == null)
                {
                    return "<div class='undefinedVariableType'>undefined</div>" +
                           $"<div class='variableLine'><span class='varName'>${variableName}</span> : <span class='varValue'>$null</span></div>";
                }

                var defaultProps = new string[0];
                if (debugVariable is PSObject)
                {
                    var script =
                        $"${variableName}.PSStandardMembers.DefaultDisplayPropertySet.ReferencedPropertyNames";
                    session.Output.SilenceOutput = true;
                    try
                    {
                        if (session.TryInvokeInRunningSession(script, out List<object> results) && results != null)
                        {
                            defaultProps = session.IsRunning
                                ? (session.Output.SilencedOutput?.ToString()
                                       .Split('\n')
                                       .Select(line => line.Trim())
                                       .ToArray() ?? new string[0])
                                : results.Cast<string>().ToArray();
                            session.Output.SilencedOutput?.Clear();
                        }
                    }
                    finally
                    {
                        session.Output.SilenceOutput = false;
                    }
                }
                var variable = debugVariable.BaseObject();
                if (variable is PSCustomObject)
                {
                    variable = debugVariable;
                }
                var details = new VariableDetails("$" + variableName, variable);
                var varValue = $"<div class='variableType'>{variable.GetType().FullName}</div>";
                varValue +=
                    $"<div class='variableLine'><span class='varName'>${variableName}</span> : <span class='varValue'>{details.HtmlEncodedValueString}</span></div>";

                if (!details.IsExpandable)
                {
                    return varValue;
                }

                // sort only if the object is not an array otherwise the indexes will get scrambled.
                var children = details.ShowDotNetProperties
                    ? details.GetChildren().OrderBy(d => d.Name).ToArray()
                    : details.GetChildren();

                foreach (var child in children)
                {
                    if (!child.IsExpandable ||
                        defaultProps.Contains(child.Name, StringComparer.OrdinalIgnoreCase) ||
                        ImportantProperties.Contains(child.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        varValue +=
                            $"<span class='varChild'><span class='childName'>{child.Name}</span> : <span class='childValue'>{child.HtmlEncodedValueString}</span></span>";
                    }
                    else
                    {
                        if (details.ShowDotNetProperties)
                        {
                            continue;
                        }
                        varValue +=
                            $"<span class='varChild'><span class='childName'>{child.Name}</span> : <span class='childValue'>{{";
                        foreach (var subChild in child.GetChildren())
                        {
                            if (!subChild.IsExpandable)
                            {
                                varValue +=
                                    $"<span class='childName'>{subChild.Name}</span> : {subChild.HtmlEncodedValueString}, ";
                            }
                        }
                        varValue = varValue.TrimEnd(' ', ',');
                        varValue += "}</span></span>";
                    }
                }
                if (details.MaxArrayParseSizeExceeded)
                {
                    varValue +=
                        $"<span class='varChild'><span class='varName'>... first {VariableDetails.MaxArrayParseSize} items shown.</span></span>";
                }
                return varValue;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }


        private static ScriptSession GetScriptSession(string guid)
        {
            return ScriptSessionManager.GetSession(guid, ApplicationNames.Console, false);
        }

        protected void RunJob(ScriptSession session, string command)
        {
            if (!IsLoggedInUserAuthorized)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrEmpty(command))
                {
                    if (session.IsRunning)
                    {
                        session.TryInvokeInRunningSession(command);
                    }
                    else
                    {
                        session.ExecuteScriptPart(command);
                    }
                }
            }
            catch (Exception ex)
            {
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager.GetContextJob();
                if (job != null)
                {
                    job.StatusFailed = true;

                    var exceptionMessage = ScriptSession.GetExceptionString(ex);
                    if (job.Options.WriteToLog)
                    {
                        PowerShellLog.Error("[Session] action=scriptExecutionFailed", ex);
                    }
                    job.AddStatusMessage(exceptionMessage);
                    job.AddStatusMessage(
                        "Uh oh, looks like the command you ran is invalid or something else went wrong. Is it something we should know about?");
                    job.AddStatusMessage(
                        "Please submit a support ticket here https://git.io/spe with error details, screenshots, and anything else that might help.");
                    job.AddStatusMessage(
                        "We also have a user guide here https://doc.sitecorepowershell.com/.");
                }
                else
                {
                    PowerShellLog.Error("[Session] action=commandJobNotFound", ex);
                }
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object PollCommandOutput(string guid, string handle, string stringFormat)
        {
            if (!IsLoggedInUserAuthorized)
            {
                return string.Empty;
            }

            HttpContext.Current.Response.ContentType = "application/json";
            var serializer = new JavaScriptSerializer();
            var session = GetScriptSession(guid);
            var result = new Result();
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var scriptJob = jobManager.GetJob(GetJobId(guid, handle));
            if (scriptJob == null)
            {
                result.status = StatusError;
                result.result =
                    "Can't find your command result. This might mean that your job has timed out or your script caused the application to restart.";
                result.prompt = $"PS {session.CurrentLocation}>";

                if (!session.Output.HasUpdates())
                {
                    session.Output.ClearSilent();
                    return serializer.Serialize(result);
                }
            }
            else
            {
                result.handle = handle;
            }

            if (scriptJob != null && scriptJob.StatusFailed)
            {
                result.status = StatusError;
                var message =
                    string.Join(Environment.NewLine, scriptJob.StatusMessages.Cast<string>().ToArray())
                        .Replace("[", "&#91;")
                        .Replace("]", "&#93;");
                result.result = "[[;#f00;#000]" +
                                (message.IsNullOrEmpty() ? "Command failed" : HttpUtility.HtmlEncode(message)) + "]";
                result.prompt = $"PS {session.CurrentLocation}>";
                session.Output.ClearSilent();
                return serializer.Serialize(result);
            }

            var complete = scriptJob == null || scriptJob.IsDone;

            // Clear-Host called from within the running script: tell the client
            // to purge its displayed output before rendering anything new.
            result.clear = session.Output.ConsumeClearPending();

            var state = GetStreamingState(guid);
            if (result.clear)
            {
                // Client is about to clear its terminal; reset our local
                // streaming tracking so we start at line 0.
                state.CommittedLineCount = 0;
                state.HasPendingPartial = false;
            }

            result.emits = new List<EmitInstruction>();
            BuildStreamingEmits(session, state, result.emits);

            // Populate the legacy `result` field with the concatenated emit
            // text so any consumer that still reads it (for example tests
            // or older clients) still sees something. The Console client
            // prefers `emits` when present.
            if (result.emits.Count > 0)
            {
                var flat = new StringBuilder();
                foreach (var e in result.emits)
                {
                    flat.Append(e.text);
                }
                result.result = flat.ToString().TrimEnd('\r', '\n');
            }
            else
            {
                result.result = string.Empty;
            }

            result.prompt = $"PS {session.CurrentLocation}>";

            // "partial" status means "there is more content that has not
            // been emitted this poll, keep polling". With the streaming
            // drain this maps to HasPendingPartial being true - the last
            // line is unterminated and may still grow.
            bool hadContent = result.emits.Count > 0;
            result.status = complete
                ? (hadContent || state.HasPendingPartial ? StatusPartial : StatusComplete)
                : StatusWorking;
            result.background = OutputLine.ProcessHtmlColor(session.PrivateData.BackgroundColor);
            result.color = OutputLine.ProcessHtmlColor(session.PrivateData.ForegroundColor);

            if (complete && !state.HasPendingPartial)
            {
                // Script is done and there is nothing left to stream.
                // ClearSilent the output buffer so future commands start
                // fresh; also reset our streaming tracking.
                session.Output.ClearSilent();
                state.CommittedLineCount = 0;
                state.HasPendingPartial = false;
            }

            var serializedResult = serializer.Serialize(result);
            return serializedResult;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object CompleteAceCommand(string guid, string command)
        {
            if (!IsLoggedInUserAuthorized ||
                !SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ISE))
            {
                return string.Empty;
            }

            PowerShellLog.Debug($"[ISE] action=autoComplete session={guid} user={Sitecore.Context.User?.Name}");

            var serializer = new JavaScriptSerializer();
            var result = serializer.Serialize(GetTabCompletionOutputs(guid, command, true));
            return result;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object CompleteCommand(string guid, string command)
        {
            if (!IsLoggedInUserAuthorized ||
                !SessionElevationManager.IsSessionTokenElevated(ApplicationNames.Console))
            {
                return string.Empty;
            }

            PowerShellLog.Debug($"[Console] action=autoComplete session={guid} user={Sitecore.Context.User?.Name}");

            var serializer = new JavaScriptSerializer();
            var result = serializer.Serialize(GetTabCompletionOutputs(guid, command, false));
            return result;
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object GetAutoCompletionPrefix(string guid, string command)
        {
            if (!IsLoggedInUserAuthorized ||
                !SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ISE))
            {
                return string.Empty;
            }

            PowerShellLog.Debug($"[Session] action=autoComplete session={guid} user={Sitecore.Context.User?.Name}");

            var serializer = new JavaScriptSerializer();
            var session = GetScriptSession(guid);
            try
            {
                var result = serializer.Serialize(CommandCompletion.GetPrefix(session, command));
                return result;
            }
            finally
            {
                if (string.IsNullOrEmpty(guid))
                {
                    ScriptSessionManager.RemoveSession(session);
                }
            }
        }

        private static string[] GetTabCompletionOutputs(string guid, string command, bool lastTokenOnly)
        {
            var session = GetScriptSession(guid);
            try
            {
                var result = CommandCompletion.FindMatches(session, command, lastTokenOnly);
                return result.ToArray();
            }
            finally
            {
                if (string.IsNullOrEmpty(guid))
                {
                    ScriptSessionManager.RemoveSession(session);
                }
            }
        }

        [WebMethod(EnableSession = true)]
        [ScriptMethod(UseHttpGet = false, ResponseFormat = ResponseFormat.Json)]
        public object GetHelpForCommand(string guid, string command)
        {
            if (!IsLoggedInUserAuthorized ||
                !SessionElevationManager.IsSessionTokenElevated(ApplicationNames.ISE))
            {
                return string.Empty;
            }

            PowerShellLog.Debug($"[Session] action=helpRequested session={guid} user={Sitecore.Context.User?.Name}");

            var serializer = new JavaScriptSerializer();
            var result = serializer.Serialize(GetHelpOutputs(guid, command));
            return result;
        }

        private static string[] GetHelpOutputs(string guid, string command)
        {
            var session = GetScriptSession(guid);
            try
            {
                var result = CommandHelp.GetHelp(session, command);
                return result.ToArray();
            }
            finally
            {
                if (string.IsNullOrEmpty(guid))
                {
                    ScriptSessionManager.RemoveSession(session);
                }
            }
        }

        public static string GetJobId(string sessionGuid, string handle)
        {
            return "PowerShell-" + sessionGuid + "-" + handle;
        }

        // TODO: Using the default JavaScript Serializer prevents us from being able to use PascalCasing.
        public class Result
        {
            public string handle;
            public string prompt;
            public string result;
            public string status;
            public string background;
            public string color;
            public bool clear;
            /// <summary>
            ///     Structured streaming emits for the Console terminal to
            ///     render output as it arrives, including inline-append
            ///     support for Write-Host -NoNewline via update() of the
            ///     currently-pending partial line.
            ///
            ///     Each entry has an op and text:
            ///         op = "append"  -> new committed visual line
            ///         op = "partial" -> start/update the pending partial
            ///         op = "commit"  -> replace pending partial with
            ///                            final text and release pending
            /// </summary>
            public List<EmitInstruction> emits;
        }

        public class EmitInstruction
        {
            public string op;
            public string text;
        }

        /// <summary>
        ///     Per-session streaming state for the Console terminal. Tracks
        ///     the next unread line index in session.Output and whether the
        ///     last emission left a pending partial on the client. Keyed by
        ///     session guid.
        /// </summary>
        private class StreamingState
        {
            public int CommittedLineCount;
            public bool HasPendingPartial;
        }

        private static readonly ConcurrentDictionary<string, StreamingState> _streamingStates
            = new ConcurrentDictionary<string, StreamingState>(StringComparer.OrdinalIgnoreCase);

        private static StreamingState GetStreamingState(string guid)
        {
            return _streamingStates.GetOrAdd(guid, _ => new StreamingState());
        }

        /// <summary>
        ///     Drain pending lines from session.Output into a list of
        ///     streaming emit instructions. The algorithm mirrors the
        ///     ISE's StreamOutputToTerminal logic:
        ///
        ///       - iterate by index from CommittedLineCount forward,
        ///       - group lines into batches ending at a terminator or the
        ///         buffer tail,
        ///       - for terminated batches emit "append" (or "commit" when
        ///         a previous pending partial is now frozen by new content
        ///         after it),
        ///       - for the unterminated tail emit "partial" and leave
        ///         CommittedLineCount at the start of the tail so the next
        ///         poll re-reads it (catches line mutation by plain Write
        ///         appending to the last unterminated line).
        ///
        ///     Does not modify OutputBuffer internals; iterates session.Output
        ///     by index via the public IList semantics. Advances the buffer's
        ///     internal updatePointer via GetHtmlUpdate at the end so other
        ///     consumers do not re-read the same content.
        /// </summary>
        private static void BuildStreamingEmits(ScriptSession session, StreamingState state, List<EmitInstruction> emits)
        {
            var output = session.Output;
            var totalLines = output.Count;
            var committed = state.CommittedLineCount;
            var hasPending = state.HasPendingPartial;

            // The buffer was cleared (e.g. ClearSilent on script end, or a
            // Clear-Host detection path called Output.Clear). Reset local
            // tracking.
            if (totalLines < committed)
            {
                committed = 0;
                hasPending = false;
            }

            // Find the boundary between terminated and unterminated content.
            // Everything up to (and including) the last terminated line is
            // committed output; anything after it is an unterminated partial
            // tail that may still grow.
            int lastTerminated = -1;
            for (int i = totalLines - 1; i >= committed; i--)
            {
                if (output[i].Terminated)
                {
                    lastTerminated = i;
                    break;
                }
            }

            // Emit all terminated content as a single append (or commit if
            // there was a pending partial). One echo() call with the full
            // concatenated jsterm string - jquery.terminal's format parser
            // handles multi-block strings correctly in a single call.
            if (lastTerminated >= committed)
            {
                var sb = new StringBuilder();
                for (int i = committed; i <= lastTerminated; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                var jsterm = sb.ToString();
                emits.Add(new EmitInstruction
                {
                    op = hasPending ? "commit" : "append",
                    text = jsterm
                });
                hasPending = false;
                committed = lastTerminated + 1;
            }

            // If there is an unterminated tail after the last terminated
            // line, emit it as a partial for Write-Host -NoNewline support.
            if (committed < totalLines)
            {
                var sb = new StringBuilder();
                for (int i = committed; i < totalLines; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                emits.Add(new EmitInstruction { op = "partial", text = sb.ToString() });
                hasPending = true;
                // Don't advance committed past the tail; next poll re-reads it.
            }

            // Advance the OutputBuffer's internal updatePointer so that
            // any other consumer (e.g. GetConsoleUpdate in code paths we
            // do not control) does not re-read the same lines. The HTML
            // result is discarded; we only care about the pointer advance.
            var _ = output.GetHtmlUpdate();

            state.CommittedLineCount = committed;
            state.HasPendingPartial = hasPending;
        }
    }
}