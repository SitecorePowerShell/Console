using System;
using System.Management.Automation;
using System.Text;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Job = Sitecore.Jobs.Job;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellRunner : BaseForm
    {
        private string someValue;
        protected JobMonitor Monitor;
        protected Scrollbox All;
        protected Literal Result;
        protected Scrollbox Promo;
        protected Literal Title;
        protected Literal HeaderText;
        protected Literal PreviousProgressValue;
        protected Literal CurrentProgressValue;
        protected Button Cancel;

        public string Script { get; set; }
        public ApplicationSettings Settings { get; set; }
        public bool NonInteractive { get; set; }

        protected Item ScriptItem { get; set; }
        protected Item CurrentItem { get; set; }

        protected Literal BackgroundColor;
        protected Literal ForegroundColor;
        protected Literal PsProgress;
        protected Literal ProgressBar;
        protected Literal PsProgressStatus;
        protected Button OkButton;
        protected Button AbortButton;

        public string PersistentId { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string itemId = WebUtil.GetQueryString("id");
            string itemDb = WebUtil.GetQueryString("db");
            string itemLang = WebUtil.GetQueryString("lang");
            string itemVer = WebUtil.GetQueryString("ver");

            string scriptId = WebUtil.GetQueryString("scriptId");
            string scriptDb = WebUtil.GetQueryString("scriptDb");

            ScriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));
            ScriptItem.Fields.ReadAll();
            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemDb))
            {
                Database db = Factory.GetDatabase(itemDb);
                if (!string.IsNullOrEmpty(itemLang))
                {
                    CurrentItem = db.GetItem(new ID(itemId), Language.Parse(itemLang), Version.Parse(itemVer));
                }
                else
                {
                    CurrentItem = db.GetItem(new ID(itemId));
                }
            }
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            PersistentId = ScriptItem[ScriptItemFieldNames.PersistentSessionId];
            HeaderText.Text = ScriptItem.DisplayName;
            if (Monitor == null)
            {
                if (!Context.ClientPage.IsEvent)
                {
                    Monitor = new JobMonitor {ID = "Monitor"};
                    Context.ClientPage.Controls.Add(Monitor);
                }
                else
                {
                    Monitor = Context.ClientPage.FindControl("Monitor") as JobMonitor;
                }
            }

            if (Context.ClientPage.IsEvent &&
                Context.ClientPage.ClientRequest.Parameters == "taskmonitor:check" &&
                PreviousProgressValue.Text != CurrentProgressValue.Text)
            {
                int percentComplete = Int32.Parse(CurrentProgressValue.Text);
                SheerResponse.Eval(
                    string.Format(@"updateProgress('#progressbar',{0});", percentComplete));
                PreviousProgressValue.Text = CurrentProgressValue.Text;
            }
        }

        [HandleMessage("psr:execute", true)]
        protected virtual void Execute(ClientPipelineArgs args)
        {
            Execute();
        }

        public void Execute()
        {
            ScriptSession scriptSession = ScriptSessionManager.GetSession(PersistentId, Settings.ApplicationName, false);
            scriptSession.SetItemLocationContext(CurrentItem);
            string contextScript = string.Format("Set-HostProperty -HostWidth {0}\n{1}",
                scriptSession.Settings.HostWidth,
                scriptSession.Settings.Prescript);

            var parameters = new object[]
            {
                scriptSession,
                contextScript,
                ScriptItem[ScriptItemFieldNames.Script]
            };
            var runner = new ScriptRunner(ExecuteInternal, parameters, false);
            Monitor.Start("ScriptExecution", "PowerShellRunner", runner.Run);
            HttpContext.Current.Session[Monitor.JobHandle.ToString()] = scriptSession;
        }

        public class RunnerOutput
        {
            public string Output { get; set; }
            public string Errors { get; set; }
            public bool HasErrors { get; set; }
        }

        protected void ExecuteInternal(params object[] parameters)
        {
            var scriptSession = parameters[0] as ScriptSession;
            var contextScript = parameters[1] as string;
            var script = parameters[2] as string;

            if (scriptSession == null || contextScript == null)
            {
                return;
            }

            try
            {
                scriptSession.ExecuteScriptPart(contextScript);
                scriptSession.ExecuteScriptPart(script);
                if (Context.Job != null)
                {
                    JobContext.Flush();
                    Context.Job.Status.Result = new RunnerOutput
                    {
                        Errors = string.Empty,
                        Output = scriptSession.Output.ToHtml(),
                        HasErrors = scriptSession.Output.HasErrors
                    };
                    object jobMessageResult = JobContext.SendMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
            catch (Exception exc)
            {
                if (Context.Job != null)
                {
                    JobContext.Flush();
                    var output = new StringBuilder(10240);
                    if (scriptSession.Output != null)
                    {
                        foreach (var outputLine in scriptSession.Output)
                        {
                            outputLine.GetHtmlLine(output);
                        }
                    }
                    Context.Job.Status.Result = new RunnerOutput
                    {
                        Errors = scriptSession.GetExceptionString(exc),
                        Output = output.ToString(),
                        HasErrors = true
                    };
                    JobContext.PostMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
        }

        [HandleMessage("psr:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            Job job = JobManager.GetJob(Monitor.JobHandle);
            var result = (RunnerOutput) job.Status.Result;
            string printResults = result.Output ?? "Script finished - no results to display.";
            if (!string.IsNullOrEmpty(result.Errors))
            {
                printResults += string.Format("<pre style='background:red;'>{0}</pre>", result.Errors);
            }
            Result.Value = printResults;
            PsProgress.Text = string.Empty;
            PsProgressStatus.Text = "<span class='status'> </span><br/>";
            SheerResponse.Eval(string.Format("scriptFinished('#progressbar',{0},{1});",
                (!string.IsNullOrEmpty(result.Output)).ToString().ToLowerInvariant(),
                result.HasErrors.ToString().ToLowerInvariant()));
            Title.Text = "Done!";
            OkButton.Visible = true;
            AbortButton.Visible = false;
            var scriptSession = (ScriptSession) HttpContext.Current.Session[Monitor.JobHandle.ToString()];
            HttpContext.Current.Session.Remove(Monitor.JobHandle.ToString());


            string scriptId = WebUtil.GetQueryString("scriptId");
            string scriptDb = WebUtil.GetQueryString("scriptDb");
            ScriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));
            PersistentId = ScriptItem[ScriptItemFieldNames.PersistentSessionId];
            if (string.IsNullOrEmpty(PersistentId))
            {
                scriptSession.Dispose();
            }
        }

        protected virtual void OkClick()
        {
            SheerResponse.CloseWindow();
        }

        protected virtual void AbortClick()
        {
            var currentSession = HttpContext.Current.Session[Monitor.JobHandle.ToString()] as ScriptSession;
            currentSession.Abort();
        }

        protected virtual void ViewResults()
        {
            string resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = Result.Value;
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellResultViewerText"));
            urlString.Add("sid", resultSig);
            ClientCommand response = SheerResponse.ShowModalDialog(urlString.ToString(), "800", "600");
            SheerResponse.CloseWindow();
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var sb = new StringBuilder();

            string activity = args.Parameters["Activity"];
            Title.Text = string.IsNullOrEmpty(activity) ? "Running script..." : activity;

            string status = args.Parameters["StatusDescription"];
            bool showStatus = !string.IsNullOrEmpty(status);
            PsProgressStatus.Visible = showStatus;
            PsProgressStatus.Text = showStatus
                ? string.Format("<span class='status'><span class='label'>Status:</status> {0}</span><br/>",
                    status)
                : "<sp-an class='status'> </span><br/>";

            if (args.Parameters["RecordType"] == ProgressRecordType.Completed.ToString())
            {
                PsProgress.Text = string.Empty;
                if (!string.IsNullOrEmpty(CurrentProgressValue.Text))
                {
                    SheerResponse.Eval(@"undeterminateProgress('#progressbar');");
                }
                CurrentProgressValue.Text = "";
                PreviousProgressValue.Text = "";
            }
            else
            {
                if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
                {
                    someValue = CurrentProgressValue.Text = args.Parameters["PercentComplete"];
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    int secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat(
                            "<span class='timeRemaining'><span class='label'>Time remaining:</span> {0:c}</span><br/>",
                            new TimeSpan(0, 0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<span class='operation'><span class='label'>Operation:</span> {0}</span>",
                        args.Parameters["CurrentOperation"]);
                }

                PsProgress.Text = sb.ToString();
            }
        }

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            Error.AssertObject(message, "message");
            base.HandleMessage(message);
            var context = new CommandContext(CurrentItem);
            foreach (var key in message.Arguments.AllKeys)
            {
                context.Parameters.Add(key, message.Arguments[key]);
            }

            Dispatcher.Dispatch(message, context);
        }
    }
}