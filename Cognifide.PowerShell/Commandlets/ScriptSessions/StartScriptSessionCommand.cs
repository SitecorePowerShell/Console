using System;
using System.Collections;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using JobManager = Sitecore.Jobs.JobManager;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Start, "ScriptSession", SupportsShouldProcess = true)]
    [OutputType(typeof (ScriptSession))]
    public class StartScriptSessionCommand : BaseScriptSessionCommand
    {
        [Parameter(ParameterSetName = "SessionID, ScriptItem", Mandatory = true)]
        [Parameter(ParameterSetName = "SessionID, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "SessionID, ScriptPath", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public override string[] Id { get; set; }

        [Parameter(ParameterSetName = "ExistingSession, ScriptItem", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptPath", Mandatory = true)]
        public override ScriptSession[] Session { get; set; }

        [Parameter(ParameterSetName = "ExistingSession, ScriptItem", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "SessionID, ScriptItem", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "NewSession, ScriptItem", Mandatory = true)]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "SessionID, ScriptPath", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptPath", Mandatory = true)]
        [Parameter(ParameterSetName = "NewSession, ScriptPath", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "SessionID, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "NewSession, ScriptBlock", Mandatory = true)]
        public ScriptBlock ScriptBlock { get; set; }

        [Parameter]
        public string JobName { get; set; }

        [Parameter]
        public Hashtable ArgumentList { get; set; }

        [Parameter]
        public AccountIdentity Identity { get; set; }

        [Parameter]
        public SwitchParameter DisableSecurity { get; set; }

        [Parameter]
        public SwitchParameter AutoDispose { get; set; }

        [Parameter]
        public SwitchParameter Interactive { get; set; }

        [Parameter]
        public Item ContextItem { get; set; }

        private ScriptBlock scriptBlock;
        private Item scriptItem;

        protected override void ProcessSession(ScriptSession session)
        {
            if (session.State == RunspaceAvailability.Busy)
            {
                WriteError(typeof(CmdletInvocationException),
                    $"The script session with Id '{session.ID}' cannot execute the script because it is Busy. Use Stop-ScriptSession or wait for the operation to complete.",
                    ErrorIds.ScriptSessionBusy, ErrorCategory.ResourceBusy, session.ID);
                return;
            }
            session.AutoDispose = AutoDispose;
            session.SetItemLocationContext(ContextItem);

            if (ArgumentList != null)
            {
                foreach (string argument in ArgumentList.Keys)
                {
                    session.SetVariable(argument, ArgumentList[argument]);
                }
            }

            var handle = string.IsNullOrEmpty(JobName) ? ID.NewID.ToString() : JobName;
            var jobOptions = new JobOptions(GetJobId(session.ID, handle), "PowerShell", "shell", this, "RunJob",
                new object[] { session, scriptBlock.ToString() })
            {
                AfterLife = new TimeSpan(0, 0, 1),
                ContextUser = Identity ?? Context.User,
                EnableSecurity = !DisableSecurity,
                ClientLanguage = Context.ContentLanguage
            };

            if (Interactive)
            {
                var appParams = new Hashtable();
                if (scriptItem != null)
                {
                    appParams.Add("scriptId", scriptItem.ID.ToString());
                    appParams.Add("scriptDb", scriptItem.Database.Name);
                }
                else
                {
                    session.JobScript = scriptBlock.ToString();
                    session.JobOptions = jobOptions;
                }
                appParams.Add("appMode", "1");
                appParams.Add("sessionKey", session.Key);

                var message = new ShowApplicationMessage("PowerShell/PowerShell Runner", "PowerShell", "", "500", "360",
                    false, appParams)
                {Title = " "};
                PutMessage(message);                
            }
            else
            {
                Sitecore.Jobs.JobManager.Start(jobOptions);
            }
            WriteObject(session);
        }

        protected override void ProcessRecord()
        {
            if (Interactive && !HostData.ScriptingHost.Interactive)
            {
                RecoverHttpContext();
                WriteError(typeof(CmdletInvocationException),
                    "An interactive script session cannot be started from non interactive script session.",
                    ErrorIds.OriginatingScriptSessionNotInteractive, ErrorCategory.InvalidOperation, HostData.ScriptingHost.SessionId);
                return;
            }

            var script = string.Empty;
            scriptItem = Item;

            if (Item != null)
            {
                scriptItem = Item;
                script = Item[ScriptItemFieldNames.Script];
            }
            else if (Path != null)
            {
                var drive = IsCurrentDriveSitecore ? CurrentDrive : ApplicationSettings.ScriptLibraryDb;

                scriptItem = PathUtilities.GetItem(Path, drive, ApplicationSettings.ScriptLibraryPath);

                if (scriptItem == null)
                {
                    WriteError(typeof (ItemNotFoundException), $"The script '{Path}' cannot be found.",
                        ErrorIds.ItemNotFound, ErrorCategory.ObjectNotFound, Path);
                    return;
                }
                script = scriptItem[ScriptItemFieldNames.Script];
            }
            
            if (!ShouldProcess(scriptItem?.GetProviderPath() ?? string.Empty, "Start new script session")) return;

            scriptBlock = ScriptBlock ?? InvokeCommand.NewScriptBlock(script);

            // sessions from IDs
            if (Id != null && Id.Length > 0)
            {
                foreach (var id in Id)
                {
                    // is id defined?
                    if (string.IsNullOrEmpty(id))
                    {
                        WriteError(typeof (ObjectNotFoundException),
                            "The script session Id cannot be null or empty.",
                            ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, Id);
                        break;
                    }
                    
                    // is it a wildcard search for session?
                    if (id.Contains("*") || id.Contains("?"))
                    {
                        if (ScriptSessionManager.SessionExistsForAnyUserSession(id))
                        {
                            ScriptSessionManager.GetMatchingSessionsForAnyUserSession(id).ForEach(ProcessSession);
                        }
                        else
                        {
                            WriteError(typeof (ObjectNotFoundException),
                                $"The script session with Id '{Id}' cannot be found.",
                                ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, Id);
                        }
                        break;
                    }
                    // does session exist?
                    if (ScriptSessionManager.SessionExistsForAnyUserSession(id))
                    {
                        ScriptSessionManager.GetMatchingSessionsForAnyUserSession(id).ForEach(ProcessSession);
                    }
                    else // OK... fine... execute in a new persistent session!
                    {
                        ProcessSession(ScriptSessionManager.GetSession(id, ApplicationNames.BackgroundJob, false));
                    }
                }

                return;
            }

            if (Session != null)
            {
                if (Session.Length == 0)
                {
                    WriteError(typeof (ObjectNotFoundException), "Script session cannot be found.",
                        ErrorIds.ScriptSessionNotFound, ErrorCategory.ResourceUnavailable, string.Empty);
                    return;
                }
                foreach (var session in Session)
                {
                    ProcessSession(session);
                }
            }

            ProcessSession(ScriptSessionManager.GetSession(string.Empty, ApplicationNames.BackgroundJob, false));
        }

        protected void RunJob(ScriptSession session, string command)
        {
            try
            {
                session.JobResultsStore = null;
                session.JobResultsStore = session.ExecuteScriptPart(command, false, false, false);

            }
            catch (Exception ex)
            {
                var job = Sitecore.Context.Job;
                if (job != null)
                {
                    job.Status.Failed = true;

                    var exceptionMessage = ScriptSession.GetExceptionString(ex);
                    if (job.Options.WriteToLog)
                    {
                        PowerShellLog.Error("Error while executing PowerShell Extensions script.", ex);
                    }
                    job.Status.Messages.Add(exceptionMessage);
                    job.Status.Messages.Add(
                        "Uh oh, looks like the command you ran is invalid or something else went wrong. Is it something we should know about?");
                    job.Status.Messages.Add(
                        "Please submit a support ticket here https://git.io/spe with error details, screenshots, and anything else that might help.");
                    job.Status.Messages.Add(
                        "We also have a user guide here http://sitecorepowershell.gitbooks.io/sitecore-powershell-extensions/.");
                }
                else
                {
                    PowerShellLog.Error("Script execution failed. Could not find command job.", ex);
                }
            }
            finally
            {
                if (session.AutoDispose)
                {
                    session.Dispose();
                }
            }
        }

        public static string GetJobId(string sessionGuid, string handle)
        {
            return "PowerShell-Background-" + sessionGuid + "-" + handle;
        }

    }
}