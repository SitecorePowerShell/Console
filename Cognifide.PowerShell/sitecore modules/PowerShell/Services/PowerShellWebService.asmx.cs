using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.Script.Services;
using System.Web.Security;
using System.Web.Services;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Cognifide.PowerShell.Core.Debugging;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Exceptions;
using Sitecore.Security.Accounts;
using Sitecore.StringExtensions;
using LicenseManager = Sitecore.SecurityModel.License.LicenseManager;
using PSCustomObject = System.Management.Automation.PSCustomObject;
using PSObject = System.Management.Automation.PSObject;

namespace Cognifide.PowerShell.Console.Services
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

            if (Sitecore.Context.IsLoggedIn)
            {
                if (Sitecore.Context.User.Name.Equals(userName, StringComparison.OrdinalIgnoreCase))
                    return true;
                Sitecore.Context.Logout();
            }
            
            if (!LicenseManager.HasContentManager && !LicenseManager.HasExpress)
                throw new AccessDeniedException("A required license is missing");
            Assert.IsTrue(Membership.ValidateUser(userName, password), "Unknown username or password.");
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

            PowerShellLog.Info($"Arbitrary script execution in Console session '{guid}' by user: '{Sitecore.Context.User?.Name}'");

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
                        PowerShellLog.Error("Error while executing PowerShell Extensions script.", ex);
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
                    PowerShellLog.Error("Script execution failed. Could not find command job.", ex);
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
                    session.Output.Clear();
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
                session.Output.Clear();
                return serializer.Serialize(result);
            }

            var complete = scriptJob == null || scriptJob.IsDone;

            var output = new StringBuilder();
            session.Output.GetConsoleUpdate(output, 131072);
            var partial = session.Output.HasUpdates();
            result.result = output.ToString().TrimEnd('\r', '\n');
            result.prompt = $"PS {session.CurrentLocation}>";

            result.status = complete ? (partial ? StatusPartial : StatusComplete) : StatusWorking;
            result.background = OutputLine.ProcessHtmlColor(session.PrivateData.BackgroundColor);
            result.color = OutputLine.ProcessHtmlColor(session.PrivateData.ForegroundColor);

            if (partial && complete)
            {
                session.Output.Clear();
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

            PowerShellLog.Info($"Auto completion requested for command in ISE session '{guid}' by user: '{Sitecore.Context.User?.Name}'");

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

            PowerShellLog.Info($"Auto completion requested for command in Console session '{guid}' by user: '{Sitecore.Context.User?.Name}'");

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

            PowerShellLog.Info($"Auto completion requested in session '{guid}' by user: '{Sitecore.Context.User?.Name}'");

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

            PowerShellLog.Info($"Help message requested in session '{guid}' by user: '{Sitecore.Context.User?.Name}'");

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

        }
    }
}