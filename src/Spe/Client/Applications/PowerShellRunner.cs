using System;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Web;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Client.Controls;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Version = Sitecore.Data.Version;

namespace Spe.Client.Applications
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
            get => StringUtil.GetString(ServerProperties["PersistentId"]);
            set => ServerProperties["PersistentId"] = value;
        }

        public string ScriptContent
        {
            get => StringUtil.GetString(ServerProperties["ScriptContent"]);
            set => ServerProperties["ScriptContent"] = value;
        }

        public string ItemDb
        {
            get => StringUtil.GetString(ServerProperties["ItemDb"]);
            set => ServerProperties["ItemDb"] = value;
        }

        public string ItemId
        {
            get => StringUtil.GetString(ServerProperties["ItemId"]);
            set => ServerProperties["ItemId"] = value;
        }

        public string ItemLang
        {
            get => StringUtil.GetString(ServerProperties["ItemLang"]);
            set => ServerProperties["ItemLang"] = value;
        }

        public string ItemVer
        {
            get => StringUtil.GetString(ServerProperties["ItemVer"]);
            set => ServerProperties["ItemVer"] = value;
        }

        public string PageId
        {
            get => StringUtil.GetString(ServerProperties["PageId"]);
            set => ServerProperties["PageId"] = value;
        }

        public string PageLang
        {
            get => StringUtil.GetString(ServerProperties["PageLang"]);
            set => ServerProperties["PageLang"] = value;
        }

        public string PageVer
        {
            get => StringUtil.GetString(ServerProperties["PageVer"]);
            set => ServerProperties["PageVer"] = value;
        }

        public bool CallerFullScreen
        {
            get => StringUtil.GetString(ServerProperties["CallerFullScreen"]) == "1";
            set => ServerProperties["CallerFullScreen"] = value ? "1" : string.Empty;
        }
        
        public bool HasScript
        {
            get => StringUtil.GetString(ServerProperties["HasScript"]) == "1";
            set => ServerProperties["HasScript"] = value ? "1" : string.Empty;
        }

        public string ScriptId
        {
            get => StringUtil.GetString(ServerProperties["ScriptId"]);
            set => ServerProperties["ScriptId"] = value;
        }

        public string RenderingId
        {
            get => StringUtil.GetString(ServerProperties["RenderingId"]);
            set => ServerProperties["RenderingId"] = value;
        }


        public string ScriptDb
        {
            get => StringUtil.GetString(ServerProperties["ScriptDb"]);
            set => ServerProperties["ScriptDb"] = value;
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
            get => Monitor.Active;
            set => Monitor.Active = value;
        }

        public string RedirectUrl
        {
            get => StringUtil.GetString(ServerProperties["RedirectUrl"]);
            set => ServerProperties["RedirectUrl"] = value;
        }

        // Set once the job finished or was lost. Progress messages drained in
        // the same poll as the completion (a tab that caught up after sleeping)
        // run after MonitorOnJobFinished and must not repaint the progress.
        public bool Finished
        {
            get => StringUtil.GetString(ServerProperties["Finished"]) == "1";
            set => ServerProperties["Finished"] = value ? "1" : string.Empty;
        }

        // Header of the script being run, kept for a Runner that re-attaches.
        public string ScriptTitle
        {
            get => StringUtil.GetString(ServerProperties["ScriptTitle"]);
            set => ServerProperties["ScriptTitle"] = value;
        }

        public string ScriptIcon
        {
            get => StringUtil.GetString(ServerProperties["ScriptIcon"]);
            set => ServerProperties["ScriptIcon"] = value;
        }

        // Set when the Runner was opened to re-attach to a job another page
        // started (?reattach=<job handle>), instead of running a script.
        public string ReattachHandle
        {
            get => StringUtil.GetString(ServerProperties["ReattachHandle"]);
            set => ServerProperties["ReattachHandle"] = value;
        }

        // Lease discovery already claimed for this Runner (?claim=). Empty
        // when the Runner reloaded itself after losing contact with the job.
        public string ReattachClaim
        {
            get => StringUtil.GetString(ServerProperties["ReattachClaim"]);
            set => ServerProperties["ReattachClaim"] = value;
        }

        protected override void OnLoad(EventArgs e)
        {
            if (ServiceAuthorizationManager.TerminateUnauthorizedRequest(WebServiceSettings.ServiceExecution,
                Context.User?.Name))
            {
                PowerShellLog.Audit($"[Runner] action=accessDenied target=ScriptRunnerDialog");
                return;
            }

            base.OnLoad(e);
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);

            if (!Context.ClientPage.IsEvent && !string.IsNullOrEmpty(WebUtil.GetQueryString("reattach")))
            {
                LoadReattachContext();
            }
            else if (!Context.ClientPage.IsEvent)
            {
                ItemId = WebUtil.GetQueryString("id");
                ItemDb = WebUtil.GetQueryString("db");
                ItemLang = WebUtil.GetQueryString("lang");
                ItemVer = WebUtil.GetQueryString("ver");

                PageId = WebUtil.GetQueryString("pageId");
                PageLang = WebUtil.GetQueryString("pageLang");
                PageVer = WebUtil.GetQueryString("pageVer");

                CallerFullScreen = WebUtil.GetQueryString("cfs") == "1";
                HasScript = WebUtil.GetQueryString("HasScript") == "1";

                ScriptId = WebUtil.GetQueryString("scriptId");
                ScriptDb = WebUtil.GetQueryString("scriptDb");

                RenderingId = WebUtil.GetQueryString("RenderingId");

                RedirectUrl = WebUtil.GetQueryString("redirectUrl");

                ResultsError.Text = Texts.PowerShellRunner_OnLoad_View_script_results_and_errors;
                ResultsOK.Text = Texts.PowerShellRunner_OnLoad_View_script_results;
                Copyright.Alt = Texts.PowerShellRunner_OnLoad_Show_copyright__;
                Title.Text = Texts.PowerShellRunner_UpdateProgress_Running_script___;

                if (!ScriptId.IsNullOrEmpty() && !ScriptDb.IsNullOrEmpty())
                {
                    var scriptItem = Factory.GetDatabase(ScriptDb).GetItem(new ID(ScriptId));
                    if (!scriptItem.IsPowerShellScript())
                    {
                        Title.Text = Texts.General_Operation_failed_wrong_data_template;
                        DialogHeader.Text = Texts.PowerShellSessionElevation_Execution_prevented;
                        AbortButton.Header = Sitecore.Texts.OK;
                        return;
                    }
                    scriptItem.Fields.ReadAll();
                    Icon.Src = scriptItem.Appearance.Icon;
                    ScriptIcon = scriptItem.Appearance.Icon;
                    ScriptTitle = scriptItem.DisplayName;

                    PersistentId = WebUtil.GetQueryString("sessionKey").IfNullOrEmpty(scriptItem[Templates.Script.Fields.PersistentSessionId]);

                    ScriptContent = scriptItem[Templates.Script.Fields.ScriptBody];
                    DialogHeader.Text = scriptItem.DisplayName;
                }
                else
                {
                    PersistentId = string.IsNullOrEmpty(WebUtil.GetQueryString("sessionKey"))
                        ? string.Empty
                        : HttpUtility.UrlDecode(WebUtil.GetQueryString("sessionKey"));
                    ScriptContent = ScriptSessionManager.GetSession(PersistentId).JobScript;
                }
            }
            else
            {
                if (Monitor == null)
                {
                    Monitor = Context.ClientPage.FindControl("Monitor") as SpeJobMonitor;
                }

                if (Context.ClientPage.ClientRequest.Parameters.StartsWith("pstaskmonitor:check") &&
                    PreviousProgressValue.Text != CurrentProgressValue.Text)
                {
                    var percentComplete = Int32.Parse(CurrentProgressValue.Text);
                    SheerResponse.Eval($@"spe.updateProgress('#progressbar',{percentComplete});");
                    PreviousProgressValue.Text = CurrentProgressValue.Text;
                }
            }

            if (Monitor == null && !Context.ClientPage.IsEvent)
            {
                Monitor = new SpeJobMonitor {ID = "Monitor"};
                Context.ClientPage.Controls.Add(Monitor);
            }

            Monitor.JobFinished += MonitorOnJobFinished;
            Monitor.JobDisappeared += MonitorOnJobDisappeared;
            Monitor.LeaseLost += MonitorOnLeaseLost;
        }

        private void LoadReattachContext()
        {
            ReattachHandle = WebUtil.GetQueryString("reattach");
            ReattachClaim = WebUtil.GetQueryString("claim");
            ResultsError.Text = Texts.PowerShellRunner_OnLoad_View_script_results_and_errors;
            ResultsOK.Text = Texts.PowerShellRunner_OnLoad_View_script_results;
            Copyright.Alt = Texts.PowerShellRunner_OnLoad_Show_copyright__;
            Title.Text = Texts.PowerShellRunner_UpdateProgress_Running_script___;

            // Only the header is restored here; ownership is checked when
            // psr:execute claims or adopts the lease.
            var entry = InteractiveJobRegistry.Get(ReattachHandle);
            if (entry == null || !InteractiveJobRegistry.BelongsTo(ReattachHandle, Context.User.Name)) return;

            var launch = entry.LaunchContext;
            ItemId = launch["id"];
            ItemDb = launch["db"];
            ItemLang = launch["lang"];
            ItemVer = launch["ver"];
            PageId = launch["pageId"];
            PageLang = launch["pageLang"];
            PageVer = launch["pageVer"];
            ScriptId = launch["scriptId"];
            ScriptDb = launch["scriptDb"];
            RenderingId = launch["RenderingId"];
            RedirectUrl = launch["redirectUrl"];
            PersistentId = launch["persistentId"];
            CallerFullScreen = launch["cfs"] == "1";
            ScriptTitle = launch["title"];
            ScriptIcon = launch["icon"];
            DialogHeader.Text = ScriptTitle;
            if (!string.IsNullOrEmpty(ScriptIcon))
            {
                Icon.Src = ScriptIcon;
            }
        }

        private NameValueCollection GetLaunchContext()
        {
            return new NameValueCollection
            {
                ["id"] = ItemId,
                ["db"] = ItemDb,
                ["lang"] = ItemLang,
                ["ver"] = ItemVer,
                ["pageId"] = PageId,
                ["pageLang"] = PageLang,
                ["pageVer"] = PageVer,
                ["scriptId"] = ScriptId,
                ["scriptDb"] = ScriptDb,
                ["RenderingId"] = RenderingId,
                ["redirectUrl"] = RedirectUrl,
                ["persistentId"] = PersistentId,
                ["cfs"] = CallerFullScreen ? "1" : string.Empty,
                ["title"] = ScriptTitle,
                ["icon"] = ScriptIcon
            };
        }

        private void Reattach()
        {
            var user = Context.User.Name;
            var handle = ReattachHandle;
            var entry = string.IsNullOrEmpty(ReattachClaim)
                ? InteractiveJobRegistry.TryClaim(handle, user, InteractiveJobRegistry.CurrentTabId)
                : InteractiveJobRegistry.TryAdopt(handle, user, ReattachClaim);

            if (entry == null)
            {
                var existing = InteractiveJobRegistry.Get(handle);
                if (existing != null && InteractiveJobRegistry.BelongsTo(handle, user))
                {
                    ShowMoved();
                    return;
                }

                if (existing != null)
                {
                    PowerShellLog.Audit($"[Runner] action=reattachDenied user={user} job={handle}");
                }

                ShowStopped(Texts.PowerShellRunner_ScriptSessionLost);
                return;
            }

            PowerShellLog.Audit($"[Runner] action=reattached user={user} job={handle}");
            Monitor.Adopt(entry);
        }

        private void MonitorOnJobDisappeared(object sender, EventArgs e)
        {
            ShowStopped(Texts.PowerShellRunner_ScriptSessionLost);
        }

        private void MonitorOnLeaseLost(object sender, EventArgs e)
        {
            ShowMoved();
        }

        private void ShowStopped(string message)
        {
            Finished = true;
            Title.Text = message;
            PsProgress.Text = string.Empty;
            PsProgressStatus.Text = "<span class='status'> </span><br/>";
            OkButton.Visible = true;
            AbortButton.Visible = false;
            SheerResponse.Eval("spe.scriptFinished('#progressbar',false,false);");
        }

        private void ShowMoved()
        {
            // Another window owns the job now. Forget the session so OK can
            // never dispose a session that window is still running.
            PersistentId = string.Empty;
            Monitor.SessionID = string.Empty;
            ShowStopped(Texts.PowerShellRunner_ScriptShownElsewhere);
        }

        [HandleMessage("psr:execute", true)]
        protected virtual void Execute(ClientPipelineArgs args)
        {
            Execute();
        }

        public void Execute()
        {
            if (!string.IsNullOrEmpty(ReattachHandle))
            {
                Reattach();
                return;
            }

            var scriptSession = ScriptSessionManager.GetSession(PersistentId, Settings.ApplicationName, false);
            scriptSession.SetItemLocationContext(CurrentItem);
            var jobName = "Interactive Script Execution";
            var jobUser = Context.User;
            if (!ScriptDb.IsNullOrEmpty() && !ScriptId.IsNullOrEmpty())
            {
                var scriptItem = Factory.GetDatabase(ScriptDb).GetItem(new ID(ScriptId));
                jobName = $"SPE - \"{scriptItem?.Name}\"";
                scriptSession.SetExecutedScript(scriptItem);

                if (DelegatedAccessManager.IsElevated(jobUser, scriptItem))
                {
                    jobUser = DelegatedAccessManager.GetDelegatedUser(jobUser, scriptItem);
                    PowerShellLog.Audit($"[DelegatedAccess] action=executing impersonatedUser={jobUser.Name} script=\"{scriptItem.Name} {scriptItem.ID}\" source=Runner");
                }
                else
                {
                    PowerShellLog.Audit($"[Runner] action=executing user={jobUser.Name} script=\"{scriptItem.Name} {scriptItem.ID}\"");
                }
            }
            else
            {
                PowerShellLog.Audit($"[Runner] action=executing user={Context.User.Name} script=arbitrary");
            }
            if (scriptSession.JobOptions != null)
            {
                jobName = scriptSession.JobOptions?.JobName;
            }
            scriptSession.SetVariable("Page", PageItem);
            scriptSession.SetVariable("RenderingId", RenderingId);
            scriptSession.SetVariable("SitecoreFullScreen", CallerFullScreen);
            scriptSession.Interactive = true;

            var runner = new ScriptRunner(ExecuteInternal, scriptSession, ScriptContent,
                string.IsNullOrEmpty(PersistentId));
            Monitor.Start(jobName, "PowerShellRunner", runner.Run, Context.Language, jobUser,
                scriptSession.JobOptions, InteractiveJobOrigin.Runner, GetLaunchContext());
            Monitor.SessionID = scriptSession.Key;
        }

        protected void ExecuteInternal(ScriptSession scriptSession, string script)
        {
            scriptSession.ExecuteScriptPart(script);
        }

        private void MonitorOnJobFinished(object sender, EventArgs e)
        {
            var args = e as SessionCompleteEventArgs;
            var result = args?.RunnerOutput;
            Finished = true;

            // Result.Value is handed to PowerShellResultViewerText which hosts
            // a jquery.terminal. Keeping the content in jsterm format (#1458)
            // lets the terminal render it with its own DOM virtualisation,
            // which handles very long script output without blowing up the
            // viewer's DOM the way an equivalent block of rendered HTML
            // spans would.
            var printResults = result?.Output ??
                               Texts.PowerShellRunner_UpdateResults_Script_finished___no_results_to_display_;
            if (result?.Exception != null)
            {
                var error = ScriptSession.GetExceptionString(result.Exception);
                // Match the soft-red error style the ISE and Console terminals
                // use (#1464 / #1471): light red foreground on the terminal
                // default background. The error is wrapped in a jsterm color
                // block so jquery.terminal renders it in the same pass as the
                // rest of the output. [ and ] need escaping so multi-bracket
                // error messages don't break jsterm parsing.
                var escaped = HttpUtility.HtmlEncode(error)
                    .Replace("[", "&#91;")
                    .Replace("]", "&#93;");
                printResults += $"\r\n[[;#FF9494;]{escaped}]\r\n";
            }
            Result.Value = printResults;
                
            PsProgress.Text = string.Empty;
            PsProgressStatus.Text = "<span class='status'> </span><br/>";

            SheerResponse.Eval(
                $"spe.scriptFinished('#progressbar',{(!string.IsNullOrEmpty(result.Output)).ToString().ToLowerInvariant()},{result.HasErrors.ToString().ToLowerInvariant()});");

            Title.Text = "Done!";
            OkButton.Visible = true;
            AbortButton.Visible = false;
            Monitor.SessionID = string.Empty;
            if (result.DeferredMessages.Any() || result.DialogResult != null)
            {
                ScriptExecutionResult dialogResult = new ScriptExecutionResult(result);
                SheerResponse.SetDialogValue(dialogResult.ToString());
            }
            if (result.CloseRunner)
            {
                if (Closed != null)
                {
                    Closed.Text = "close";
                }

                OkClick();
            }

            if (!string.IsNullOrEmpty(RedirectUrl))
            {
                SheerResponse.SetLocation(RedirectUrl);
            }
        }

        [HandleMessage("psr:close", true)]
        protected virtual void OkClick()
        {
            var sessionId = PersistentId;
            if (ScriptSessionManager.GetSessionIfExists(sessionId) is ScriptSession currentSession &&
                currentSession.AutoDispose)
            {
                currentSession.Dispose();
            }

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
            OkClick();
        }

        protected virtual void AbortClick()
        {
            var sessionId = Monitor.SessionID;
            if (ScriptSessionManager.GetSessionIfExists(sessionId) is ScriptSession currentSession)
            {
                currentSession.Abort();
            }
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
            if (Finished) return;

            var sb = new StringBuilder();

            var activity = args.Parameters["Activity"];
            Title.Text = string.IsNullOrEmpty(activity) ? Texts.PowerShellRunner_UpdateProgress_Running_script___ : activity;

            var status = args.Parameters["StatusDescription"];
            var showStatus = !string.IsNullOrEmpty(status);

            PsProgressStatus.Visible = showStatus;

            PsProgressStatus.Text = showStatus
                ? $"<span class='status'>{status}   </span><br/>"
                : "<span class='status'> </span><br/>";
            if (args.Parameters["RecordType"] == ProgressRecordType.Completed.ToString())
            {
                PsProgress.Text = string.Empty;

                if (!string.IsNullOrEmpty(CurrentProgressValue.Text))
                {
                    var percentComplete = int.Parse(CurrentProgressValue.Text);
                    percentComplete = Math.Max(percentComplete, 100);
                    SheerResponse.Eval($@"spe.updateProgress('#progressbar',{percentComplete});");
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
                    var secondsRemaining = int.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat(
                            "<span class='timeRemaining'><span class='label'>" +
                            Texts.PowerShellRunner_UpdateProgress_Time_remaining_.Translate() +
                            "</span> {0:c}</span><br/>",
                            new TimeSpan(0, 0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<span class='operation'>{0}</span>",
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