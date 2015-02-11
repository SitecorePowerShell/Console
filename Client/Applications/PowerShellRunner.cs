using System;
using System.Management.Automation;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
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

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellRunner : BaseForm, IPowerShellRunner
    {
        public SpeJobMonitor Monitor { get; private set; }
        protected Scrollbox All;
        protected Literal Result;
        protected Literal Title;
        protected Literal DialogHeader;
        protected Literal PreviousProgressValue;
        protected Literal CurrentProgressValue;
        protected Literal Closed;
        protected Button Cancel;
        protected ThemedImage Icon;

        public string Script { get; set; }
        public ApplicationSettings Settings { get; set; }
        public bool NonInteractive { get; set; }

        //protected Item CurrentItem { get; set; }

        protected Literal BackgroundColor;
        protected Literal ForegroundColor;
        protected Literal PsProgress;
        protected Literal ProgressBar;
        protected Literal PsProgressStatus;
        protected Button OkButton;
        protected Button AbortButton;

        public string PersistentId
        {
            get { return StringUtil.GetString(ServerProperties["PersistentId"]); }
            set { ServerProperties["PersistentId"] = value; }
        }

        public string ScriptContent
        {
            get { return StringUtil.GetString(ServerProperties["ScriptContent"]); }
            set { ServerProperties["ScriptContent"] = value; }
        }

        public string ItemId
        {
            get { return StringUtil.GetString(ServerProperties["ItemId"]); }
            set { ServerProperties["ItemId"] = value; }
        }

        public string ItemDb
        {
            get { return StringUtil.GetString(ServerProperties["ItemDb"]); }
            set { ServerProperties["ItemDb"] = value; }
        }

        public string ScriptId
        {
            get { return StringUtil.GetString(ServerProperties["ScriptId"]); }
            set { ServerProperties["ScriptId"] = value; }
        }

        public string ScriptDb
        {
            get { return StringUtil.GetString(ServerProperties["ScriptDb"]); }
            set { ServerProperties["ScriptDb"] = value; }
        }

        public string ItemLang
        {
            get { return StringUtil.GetString(ServerProperties["ItemLang"]); }
            set { ServerProperties["ItemLang"] = value; }
        }

        public string ItemVer
        {
            get { return StringUtil.GetString(ServerProperties["ItemVer"]); }
            set { ServerProperties["ItemVer"] = value; }
        }

        protected Item CurrentItem
        {
            get
            {
                if (string.IsNullOrEmpty(ItemId) || string.IsNullOrEmpty(ItemDb)) return null;
                var db = Factory.GetDatabase(ItemDb);
                return !string.IsNullOrEmpty(ItemLang)
                    ? db.GetItem(new ID(ItemId), Language.Parse(ItemLang), Version.Parse(ItemVer))
                    : db.GetItem(new ID(ItemId));
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            
            if (!Context.ClientPage.IsEvent)
            {
                ItemId = WebUtil.GetQueryString("id");
                ItemDb = WebUtil.GetQueryString("db");
                ItemLang = WebUtil.GetQueryString("lang");
                ItemVer = WebUtil.GetQueryString("ver");

                ScriptId = WebUtil.GetQueryString("scriptId");
                ScriptDb = WebUtil.GetQueryString("scriptDb");

                Item scriptItem = Factory.GetDatabase(ScriptDb).GetItem(new ID(ScriptId));
                scriptItem.Fields.ReadAll();
                Icon.Src = scriptItem.Appearance.Icon;

                PersistentId = scriptItem[ScriptItemFieldNames.PersistentSessionId];
                ScriptContent = scriptItem[ScriptItemFieldNames.Script];
                DialogHeader.Text = scriptItem.DisplayName;

                if (Monitor == null)
                {
                    Monitor = new SpeJobMonitor {ID = "Monitor"};
                    Context.ClientPage.Controls.Add(Monitor);
                }
            }
            else
            {
                if (Monitor == null)
                {
                    Monitor = Context.ClientPage.FindControl("Monitor") as SpeJobMonitor;
                }

                if (Context.ClientPage.ClientRequest.Parameters == "pstaskmonitor:check" &&
                    PreviousProgressValue.Text != CurrentProgressValue.Text)
                {
                    int percentComplete = Int32.Parse(CurrentProgressValue.Text);
                    SheerResponse.Eval(
                        string.Format(@"updateProgress('#progressbar',{0});", percentComplete));
                    PreviousProgressValue.Text = CurrentProgressValue.Text;
                }
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

            scriptSession.SetExecutedScript(ScriptDb,ScriptId);
            var parameters = new object[]
            {
                scriptSession,
                contextScript,
                ScriptContent
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
                    Context.Job.Status.Result = new RunnerOutput
                    {
                        Errors = string.Empty,
                        Output = scriptSession.Output.ToHtml(),
                        HasErrors = scriptSession.Output.HasErrors
                    };
                    JobContext.PostMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
            catch (Exception exc)
            {
                Log.Error("Exception while running script", exc, this);
                if (Context.Job != null)
                {
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
            finally
            {
                if (scriptSession.CloseRunner && scriptSession.AutoDispose)
                {
                    scriptSession.Dispose();
                }
            }
        }

        [HandleMessage("psr:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            Job job = JobManager.GetJob(Monitor.JobHandle);
            var result = (RunnerOutput) job.Status.Result;
            string printResults = (result != null ? result.Output : null) ?? "Script finished - no results to display.";
            if (result != null && !string.IsNullOrEmpty(result.Errors))
            {
                printResults += string.Format("<pre style='background:red;'>{0}</pre>", result.Errors);
            }
            Result.Value = printResults;
            PsProgress.Text = string.Empty;
            PsProgressStatus.Text = "<span class='status'> </span><br/>";
            if (result != null)
            {
                SheerResponse.Eval(string.Format("scriptFinished('#progressbar',{0},{1});",
                    (!string.IsNullOrEmpty(result.Output)).ToString().ToLowerInvariant(),
                    result.HasErrors.ToString().ToLowerInvariant()));
            }
            Title.Text = "Done!";
            OkButton.Visible = true;
            AbortButton.Visible = false;
            var scriptSession = (ScriptSession) HttpContext.Current.Session[Monitor.JobHandle.ToString()];
            HttpContext.Current.Session.Remove(Monitor.JobHandle.ToString());

            if (scriptSession.CloseRunner)
            {
                scriptSession.CloseRunner = false;
                Closed.Text = "close";
                Context.ClientPage.ClientResponse.CloseWindow();
            }
            if (string.IsNullOrEmpty(PersistentId))
            {
                scriptSession.Dispose();
            }
        }

        [HandleMessage("psr:close", true)]
        protected virtual void OkClick()
        {
            SheerResponse.CloseWindow();
        }

        [HandleMessage("psr:delayedclose", true)]
        protected virtual void DelayedClose(ClientPipelineArgs args)
        {
            SheerResponse.Timer("psr:dodelayedclose", 10);
        }

        [HandleMessage("psr:dodelayedclose", true)]
        protected virtual void DoDelayedClose(ClientPipelineArgs args)
        {
            SheerResponse.CloseWindow();
        }


        protected virtual void AbortClick()
        {
            var currentSession = HttpContext.Current.Session[Monitor.JobHandle.ToString()] as ScriptSession;
            if (currentSession != null) currentSession.Abort();
        }

        protected virtual void ViewResults()
        {
            string resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = Result.Value;
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellResultViewerText"));
            urlString.Add("sid", resultSig);
            SheerResponse.ShowModalDialog(urlString.ToString(), "800", "600");
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
                ? string.Format("<span class='status'>{0}</span><br/>", status)
                : "<span class='status'> </span><br/>";

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
                    CurrentProgressValue.Text = args.Parameters["PercentComplete"];
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