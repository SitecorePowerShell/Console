using System;
using System.Collections;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;

namespace Cognifide.PowerShell.Commandlets.ScriptSessions
{
    [Cmdlet(VerbsLifecycle.Start, "ScriptSession", SupportsShouldProcess = true)]
    [OutputType(typeof (ScriptSession))]
    public class StartScriptSessionCommand : BaseCommand
    {
        private const string ParameterSetNameFromItem = "Item, ID";
        private const string ParameterSetNameFromFullPath = "Path";

        [Parameter(ParameterSetName = "SessionID, ScriptItem", Mandatory = true)]
        [Parameter(ParameterSetName = "SessionID, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "SessionID, ScriptPath", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public virtual string Id { get; set; }

        [Parameter(ParameterSetName = "ExistingSession, ScriptItem", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptBlock", Mandatory = true)]
        [Parameter(ParameterSetName = "ExistingSession, ScriptPath", Mandatory = true)]
        public virtual ScriptSession Session { get; set; }

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

        [Parameter()]
        public string JobName { get; set; }

        [Parameter]
        public Hashtable ArgumentList { get; set; }

        [Parameter]
        public AccountIdentity Identity { get; set; }

        [Parameter]
        public SwitchParameter DisableSecurity { get; set; }

        [Parameter]
        public SwitchParameter AutoDispose { get; set; }

        // Methods
        protected override void ProcessRecord()
        {
            var script = string.Empty;
            var scriptItem = Item;

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
                    var error = $"The script '{Path}' is cannot be found.";
                    WriteError(new ErrorRecord(new ItemNotFoundException(error), error, ErrorCategory.ObjectNotFound,
                        Path));
                    return;
                }
                script = scriptItem[ScriptItemFieldNames.Script];
            }
            
            if (!ShouldProcess(scriptItem?.GetProviderPath()??string.Empty, "Invoke script")) return;

            var scriptBlock = ScriptBlock ?? InvokeCommand.NewScriptBlock(script);

            var session =
                Session ??
                (!string.IsNullOrEmpty(Id) && ScriptSessionManager.SessionExists(Id)
                    ? ScriptSessionManager.GetSession(Id)
                    : ScriptSessionManager.GetSession(Id ?? string.Empty, ApplicationNames.BackgroundJob, false));

            session.AutoDispose = AutoDispose;

            if (ArgumentList != null)
            {
                foreach (string argument in ArgumentList.Keys)
                {
                    session.SetVariable(argument,ArgumentList[argument]);
                }
            }

            var handle = string.IsNullOrEmpty(JobName) ? ID.NewID.ToString() : JobName;
            var jobOptions = new JobOptions(GetJobId(session.ID, handle), "PowerShell", "shell", this, "RunJob",
                new object[] { session, scriptBlock.ToString() })
            {
                AfterLife = new TimeSpan(0, 0, 1),
                ContextUser = Identity ?? Sitecore.Context.User,
                EnableSecurity = !DisableSecurity,
                ClientLanguage = Sitecore.Context.ContentLanguage
            };

            JobManager.Start(jobOptions);
            WriteObject(session);
        }
        protected void RunJob(ScriptSession session, string command)
        {
            try
            {
                session.AsyncResultsStore = null;
                session.AsyncResultsStore = session.ExecuteScriptPart(command, false, false, false);

            }
            catch (Exception ex)
            {
                var job = Sitecore.Context.Job;
                if (job != null)
                {
                    job.Status.Failed = true;

                    var exceptionMessage = session.GetExceptionString(ex);
                    if (job.Options.WriteToLog)
                    {
                        Log.Error(exceptionMessage, this);
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
                    Log.Error("Script execution failed. Could not find command job.", ex, this);
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