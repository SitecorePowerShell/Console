using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Cognifide.PowerShell.Console.Services;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellConsole : BaseForm, IPowerShellRunner
    {
        protected Literal Options;
        protected Literal Progress;
        protected Border ProgressOverlay;
        public ApplicationSettings Settings { get; set; }
        protected GridPanel ElevationRequiredPanel;
        protected GridPanel ElevatedPanel;
        protected GridPanel ElevationBlockedPanel;
        protected Border InfoPanel;

        protected string AppName
        {
            get
            {
                var appName = StringUtil.GetString(Sitecore.Context.ClientPage.ServerProperties["AppName"]);
                return string.IsNullOrEmpty(appName) ? ApplicationNames.Console : appName;
            }
            set { Sitecore.Context.ClientPage.ServerProperties["AppName"] = value ?? ApplicationNames.Console; }
        }

        public bool MonitorActive
        {
            set
            {
                SheerResponse.Eval(@"$ise(function() { cognifide.powershell.setOptions({ monitorActive: " +
                                   (value ? "true" : "false") + @" });});");
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!SecurityHelper.CanRunApplication("PowerShell/PowerShell Console") ||
                ServiceAuthorizationManager.TerminateUnauthorizedRequest(WebServiceSettings.ServiceClient,
                    Context.User?.Name))
            {
                PowerShellLog.Error($"User {Context.User?.Name} attempt to access PowerShell Console - denied.");
                return;
            }
            base.OnLoad(e);

            UpdateWarning();

            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            var settings = ApplicationSettings.GetInstance(ApplicationNames.Console, false);

            if (!Context.ClientPage.IsEvent)
            {
                Options.Text = @"<script type='text/javascript'>" +
                               @"$ise(function() { cognifide.powershell.setOptions({ initialPoll: " +
                               WebServiceSettings.InitialPollMillis + @", maxPoll: " +
                               WebServiceSettings.MaxmimumPollMillis + @", fontSize: " + 
                               settings.FontSize + $", fontFamily: '{settings.FontFamilyStyle}' }});}});</script>" +
                               @"<style>#terminal {" +
                               $"color: {OutputLine.ProcessHtmlColor(settings.ForegroundColor)};" +
                               $"background-color: {OutputLine.ProcessHtmlColor(settings.BackgroundColor)};" +
                               $"font-family: inherit;" + "}</style>";
            }
            SheerResponse.SetDialogValue("ok");
        }

        private void UpdateWarning()
        {
            var isSessionElevated = SessionElevationManager.IsSessionTokenElevated(ApplicationNames.Console);

            var controlContent = string.Empty;
            var hidePanel = false;
            var tokenAction = SessionElevationManager.GetToken(ApplicationNames.Console).Action;
            switch (tokenAction)
            {
                case (SessionElevationManager.TokenDefinition.ElevationAction.Allow):
                    // it is always elevated
                    hidePanel = true;
                    break;
                case (SessionElevationManager.TokenDefinition.ElevationAction.Password):
                    // show that session elevation can be dropped
                    controlContent = HtmlUtil.RenderControl(isSessionElevated ? ElevatedPanel : ElevationRequiredPanel);
                    break;
                case (SessionElevationManager.TokenDefinition.ElevationAction.Block):
                    controlContent = HtmlUtil.RenderControl(ElevationBlockedPanel);
                    break;
            }

            InfoPanel.InnerHtml = controlContent;
            InfoPanel.Visible = !hidePanel;
            SheerResponse.Eval($"cognifide.powershell.showInfoPanel({(!hidePanel).ToString().ToLower()});");
        }

        [HandleMessage("item:load", true)]
        protected void LoadContentEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var parameters = new UrlString();
            parameters.Add("id", args.Parameters["id"]);
            parameters.Add("fo", args.Parameters["id"]);
            Sitecore.Shell.Framework.Windows.RunApplication("Content Editor", parameters.ToString());
        }

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            if (message.Name != "pstaskmonitor:check")
                return;

            var job =
                JobManager.GetJob(PowerShellWebService.GetJobId(message.Arguments["guid"], message.Arguments["handle"]));

            if (job != null)
            {
                IMessage iMessage;
                while (job.MessageQueue.GetMessage(out iMessage))
                {
                    iMessage.Execute();
                }
            }

            if (message.Arguments.AllKeys.Contains("finished") && message.Arguments["finished"] == "true")
            {
                ProgressOverlay.Visible = false;
            }
        }

        [HandleMessage("ise:updateprogress", true)]
        protected virtual void UpdateProgress(ClientPipelineArgs args)
        {
            var showProgress =
                !string.Equals(args.Parameters["RecordType"], "Completed", StringComparison.OrdinalIgnoreCase);
            if (showProgress && args.Parameters.AllKeys.Contains("JobId"))
            {
                var job = JobManager.GetJob(args.Parameters["JobId"]);
                showProgress = !job.IsDone;
            }
            ProgressOverlay.Visible = showProgress;
            var sb = new StringBuilder();
            if (showProgress)
            {
                ProgressOverlay.Visible = true;

                sb.AppendFormat("<h2>{0}</h2>", args.Parameters["Activity"]);
                if (!string.IsNullOrEmpty(args.Parameters["StatusDescription"]))
                {
                    sb.AppendFormat("<p>{0}</p>", args.Parameters["StatusDescription"]);
                }

                if (!string.IsNullOrEmpty(args.Parameters["PercentComplete"]))
                {
                    var percentComplete = Int32.Parse(args.Parameters["PercentComplete"]);
                    if (percentComplete > -1)
                        sb.AppendFormat("<div id='progressbar'><div style='width:{0}%'></div></div>", percentComplete);
                }

                if (!string.IsNullOrEmpty(args.Parameters["SecondsRemaining"]))
                {
                    var secondsRemaining = Int32.Parse(args.Parameters["SecondsRemaining"]);
                    if (secondsRemaining > -1)
                        sb.AppendFormat("<p><strong>{0:c} </strong> remaining.</p>",
                            new TimeSpan(0, 0, secondsRemaining));
                }

                if (!string.IsNullOrEmpty(args.Parameters["CurrentOperation"]))
                {
                    sb.AppendFormat("<p>{0}</p>", args.Parameters["CurrentOperation"]);
                }
            }

            Progress.Text = sb.ToString();

            SheerResponse.Eval(@"$ise(function() { cognifide.powershell.resetAttempts(); });");
        }

        [HandleMessage("ise:elevatesession", true)]
        protected virtual void ElevateSession(ClientPipelineArgs args)
        {
            var isSessionElevated = SessionElevationManager.IsSessionTokenElevated(ApplicationNames.Console);
            var tokenAction = SessionElevationManager.GetToken(ApplicationNames.Console).Action;
            if (!isSessionElevated)
            {
                if (tokenAction == SessionElevationManager.TokenDefinition.ElevationAction.Block)
                {
                    SheerResponse.Eval(@"$ise(function() { cognifide.powershell.bootstrap(true); });");
                }
                else
                {
                    Context.ClientPage.Start(this, nameof(SessionElevationPipeline));
                }
            }
            else
            {
                SheerResponse.Eval(@"$ise(function() { cognifide.powershell.bootstrap(false); });");
            }
        }

        public void SessionElevationPipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                var url = new UrlString(UIUtil.GetUri("control:PowerShellSessionElevation"));
                url.Parameters["app"] = ApplicationNames.Console;
                TypeResolver.Resolve<ISessionElevationWindowLauncher>().ShowSessionElevationWindow(url);
                args.WaitForPostBack(true);
            }
            else
            {
                SheerResponse.Eval(SessionElevationManager.IsSessionTokenElevated(ApplicationNames.Console)
                    ? @"$ise(function() { cognifide.powershell.bootstrap(); });"
                    : @"$ise(function() { cognifide.powershell.showUnelevated(); });");
            }
        }

        public void DropElevationButtonClick()
        {
            SessionElevationManager.DropSessionTokenElevation(ApplicationNames.Console);
            SheerResponse.Eval(@"$ise(function() { cognifide.powershell.showUnelevated(); });");

            UpdateWarning();
        }
    }
}