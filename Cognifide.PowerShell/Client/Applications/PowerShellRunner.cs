using System;
using System.Management.Automation;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.VersionDecoupling;
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
using Version = Sitecore.Data.Version;
using JobManager = Sitecore.Jobs.JobManager;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellRunner : BaseForm, IPowerShellRunner
    {
        protected Button AbortButton;
        protected Scrollbox All;
        protected Literal BackgroundColor;
        protected Button Cancel;
        protected Literal Closed;
        protected Literal CurrentProgressValue;
        protected Literal DialogHeader;
        protected Literal ForegroundColor;
        protected ThemedImage Icon;
        protected Button OkButton;
        protected Literal PreviousProgressValue;
        protected Literal ProgressBar;
        protected Literal PsProgress;
        protected Literal PsProgressStatus;
        protected Literal Subtitle;
        protected Literal Result;
        protected Literal Title;
        protected Literal ResultsError;
        protected Literal ResultsOK;
        protected Image Copyright;

        public SpeJobMonitor Monitor { get; private set; }
        public string Script { get; set; }
        public ApplicationSettings Settings { get; set; }

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

        public string ItemDb
        {
            get { return StringUtil.GetString(ServerProperties["ItemDb"]); }
            set { ServerProperties["ItemDb"] = value; }
        }

        public string ItemId
        {
            get { return StringUtil.GetString(ServerProperties["ItemId"]); }
            set { ServerProperties["ItemId"] = value; }
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

        public string PageId
        {
            get { return StringUtil.GetString(ServerProperties["PageId"]); }
            set { ServerProperties["PageId"] = value; }
        }

        public string PageLang
        {
            get { return StringUtil.GetString(ServerProperties["PageLang"]); }
            set { ServerProperties["PageLang"] = value; }
        }

        public string PageVer
        {
            get { return StringUtil.GetString(ServerProperties["PageVer"]); }
            set { ServerProperties["PageVer"] = value; }
        }

        public bool AppMode
        {
            get { return StringUtil.GetString(ServerProperties["AppMode"]) == "1"; }
            set { ServerProperties["AppMode"] = value ? "1" : string.Empty; }
        }
        
        public bool HasScript
        {
            get { return StringUtil.GetString(ServerProperties["HasScript"]) == "1"; }
            set { ServerProperties["HasScript"] = value ? "1" : string.Empty; }
        }

        public string ScriptId
        {
            get { return StringUtil.GetString(ServerProperties["ScriptId"]); }
            set { ServerProperties["ScriptId"] = value; }
        }

        public string RenderingId
        {
            get { return StringUtil.GetString(ServerProperties["RenderingId"]); }
            set { ServerProperties["RenderingId"] = value; }
        }


        public string ScriptDb
        {
            get { return StringUtil.GetString(ServerProperties["ScriptDb"]); }
            set { ServerProperties["ScriptDb"] = value; }
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

        protected Item PageItem
        {
            get
            {
                if (string.IsNullOrEmpty(PageId) || string.IsNullOrEmpty(ItemDb)) return null;
                var db = Factory.GetDatabase(ItemDb);
                return !string.IsNullOrEmpty(PageLang)
                    ? db.GetItem(new ID(PageId), Language.Parse(PageLang), Version.Parse(PageVer))
                    : db.GetItem(new ID(PageId));
            }
        }

        public bool MonitorActive
        {
            get { return Monitor.Active; }
            set { Monitor.Active = value; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            Assert.IsTrue(ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name, false), "Application access denied.");

            if (!Context.ClientPage.IsEvent)
            {
                ItemId = WebUtil.GetQueryString("id");
                ItemDb = WebUtil.GetQueryString("db");
                ItemLang = WebUtil.GetQueryString("lang");
                ItemVer = WebUtil.GetQueryString("ver");

                PageId = WebUtil.GetQueryString("pageId");
                PageLang = WebUtil.GetQueryString("pageLang");
                PageVer = WebUtil.GetQueryString("pageVer");

                AppMode = WebUtil.GetQueryString("AppMode") == "1";
                HasScript = WebUtil.GetQueryString("HasScript") == "1";

                ScriptId = WebUtil.GetQueryString("scriptId");
                ScriptDb = WebUtil.GetQueryString("scriptDb");

                RenderingId = WebUtil.GetQueryString("RenderingId");


                ResultsError.Text = Texts.PowerShellRunner_OnLoad_View_script_results_and_errors;
                ResultsOK.Text = Texts.PowerShellRunner_OnLoad_View_script_results;
                Copyright.Alt = Texts.PowerShellRunner_OnLoad_Show_copyright__;
                Title.Text = Texts.PowerShellRunner_UpdateProgress_Running_script___;

                if (!string.IsNullOrEmpty(ScriptId) && !string.IsNullOrEmpty(ScriptId))
                {
                    var scriptItem = Factory.GetDatabase(ScriptDb).GetItem(new ID(ScriptId));
                    scriptItem.Fields.ReadAll();
                    Icon.Src = scriptItem.Appearance.Icon;

                    PersistentId = string.IsNullOrEmpty(WebUtil.GetQueryString("sessionKey"))
                        ? scriptItem[ScriptItemFieldNames.PersistentSessionId]
                        : WebUtil.GetQueryString("sessionKey");

                    ScriptContent = scriptItem[ScriptItemFieldNames.Script];
                    DialogHeader.Text = scriptItem.DisplayName;
                }
                else
                {
                    PersistentId = string.IsNullOrEmpty(WebUtil.GetQueryString("sessionKey"))
                        ? string.Empty
                        : WebUtil.GetQueryString("sessionKey");
                    ScriptContent = ScriptSessionManager.GetSession(PersistentId).JobScript;
                }

                if (Monitor != null) return;

                Monitor = new SpeJobMonitor {ID = "Monitor"};
                Context.ClientPage.Controls.Add(Monitor);
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
                    var percentComplete = Int32.Parse(CurrentProgressValue.Text);
                    SheerResponse.Eval($@"updateProgress('#progressbar',{percentComplete});");
                    PreviousProgressValue.Text = CurrentProgressValue.Text;
                }
            }
            Monitor.JobFinished += MonitorOnJobFinished;
        }

        [HandleMessage("psr:execute", true)]
        protected virtual void Execute(ClientPipelineArgs args)
        {
            Execute();
        }

        public void Execute()
        {
            var scriptSession = ScriptSessionManager.GetSession(PersistentId, Settings.ApplicationName, false);
            scriptSession.SetItemLocationContext(CurrentItem);
            scriptSession.SetExecutedScript(ScriptDb, ScriptId);
            scriptSession.SetVariable("Page", PageItem);
            scriptSession.SetVariable("RenderingId", RenderingId);
            scriptSession.Interactive = true;

            var runner = new ScriptRunner(ExecuteInternal, scriptSession, ScriptContent,
                string.IsNullOrEmpty(PersistentId));
            Monitor.Start("ScriptExecution", "PowerShellRunner", runner.Run, Context.Language, Context.User,
                scriptSession.JobOptions);
            Monitor.SessionID = scriptSession.Key;
        }

        protected void ExecuteInternal(ScriptSession scriptSession, string script)
        {
            scriptSession.ExecuteScriptPart(script);
        }

        private void MonitorOnJobFinished(object sender, EventArgs e)
        {
            var args = e as SessionCompleteEventArgs;
            var result = args.RunnerOutput;

            var printResults = result?.Output ??
                               Texts.PowerShellRunner_UpdateResults_Script_finished___no_results_to_display_;
            if (result?.Exception != null)
            {
                var error = ScriptSession.GetExceptionString(result.Exception);
                printResults += $"<pre style='background:red;'>{error}</pre>";
            }
            Result.Value = printResults;
            PsProgress.Text = string.Empty;
            SitecoreVersion.V80
                .OrNewer(() => PsProgressStatus.Text = "<span class='status'> </span><br/>")
                .Else(() => Subtitle.Text = "<span class='status'> </span><br/>");

            SheerResponse.Eval(string.Format("scriptFinished('#progressbar',{0},{1});",
                (!string.IsNullOrEmpty(result.Output)).ToString().ToLowerInvariant(),
                result.HasErrors.ToString().ToLowerInvariant()));

            Title.Text = "Done!";
            OkButton.Visible = true;
            AbortButton.Visible = false;
            Monitor.SessionID = string.Empty;

            if (!result.CloseRunner)
            {
                return;
            }

            if (Closed != null)
            {
                Closed.Text = "close";
            }

            OkClick();
        }

        [HandleMessage("psr:close", true)]
        protected virtual void OkClick()
        {
            var sessionId = PersistentId;
            if (ScriptSessionManager.SessionExists(sessionId))
            {
                var currentSession = ScriptSessionManager.GetSession(sessionId);
                if (currentSession.AutoDispose)
                {
                    currentSession.Dispose();
                }
            }
            if (AppMode)
            {
                SheerResponse.Eval("window.parent.scWin.closeWindow()");
            }
            else
            {
                SheerResponse.CloseWindow();
            }

        }

        [HandleMessage("psr:delayedclose", true)]
        protected virtual void DelayedClose(ClientPipelineArgs args)
        {
            SheerResponse.Timer("psr:dodelayedclose", 10);
        }

        [HandleMessage("psr:dodelayedclose", true)]
        protected virtual void DoDelayedClose(ClientPipelineArgs args)
        {
            OkClick();
        }

        protected virtual void AbortClick()
        {
            var sessionId = Monitor.SessionID;
            var currentSession = ScriptSessionManager.GetSession(sessionId);
            if (currentSession != null) currentSession.Abort();
        }

        protected virtual void ViewResults()
        {
            var resultSig = Guid.NewGuid().ToString();
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

            var activity = args.Parameters["Activity"];
            Title.Text = string.IsNullOrEmpty(activity) ? Texts.PowerShellRunner_UpdateProgress_Running_script___ : activity;

            var status = args.Parameters["StatusDescription"];
            var showStatus = !string.IsNullOrEmpty(status);

            bool isSitecore8 = CurrentVersion.IsAtLeast(SitecoreVersion.V80);
            PsProgressStatus.Visible = showStatus && isSitecore8;
            Subtitle.Visible = showStatus && !isSitecore8;

            if (isSitecore8)
            {
                PsProgressStatus.Text = showStatus
                ? $"<span class='status'>{status}   </span><br/>"
                    : "<span class='status'> </span><br/>";
            }
            else
            {
                Subtitle.Text = showStatus
                ? string.Format("<span class='status'>"+
                Texts.PowerShellRunner_UpdateProgress_Status_+
                " {0}</span>", status)
                : "<span class='status'> </span>";
            }
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
                    var secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat(
                            "<span class='timeRemaining'><span class='label'>" +
                            Texts.PowerShellRunner_UpdateProgress_Time_remaining_ +
                            "</span> {0:c}</span><br/>",
                            new TimeSpan(0, 0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<span class='operation'><span class='label'>" +
                                    Texts.PowerShellRunner_UpdateProgress_Operation_ + 
                                    "</span> {0}</span>",
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