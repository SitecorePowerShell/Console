using System;
using System.Linq;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Console.Services;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellConsole : BaseForm, IPowerShellRunner
    {
        protected Literal Options;
        protected Literal Progress;
        protected Border ProgressOverlay;
        public ApplicationSettings Settings { get; set; }

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
            base.OnLoad(e);
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            var settings = ApplicationSettings.GetInstance(ApplicationNames.AjaxConsole, false);

            if (!Context.ClientPage.IsEvent)
            {
                Options.Text = @"<script type=""text/JavaScript"" language=""javascript"">" +
                               @"$ise(function() { cognifide.powershell.setOptions({ initialPoll: " +
                               WebServiceSettings.InitialPollMillis + @", maxPoll: " +
                               WebServiceSettings.MaxmimumPollMillis + @" });});</script>" +
                               @"<style>.terminal, .terminal .terminal-output, .terminal .terminal-output div," +
                               @".terminal .terminal-output div div, .cmd, .terminal .cmd span, .terminal .cmd div {" +
                               @"color: " + OutputLine.ProcessHtmlColor(settings.ForegroundColor) + ";" +
                               @"background-color: " + OutputLine.ProcessHtmlColor(settings.BackgroundColor) +
                               ";}</style>";
            }
        }

        [HandleMessage("item:load", true)]
        protected void LoadContentEditor(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var parameters = new UrlString();
            parameters.Add("id", args.Parameters["id"]);
            parameters.Add("fo", args.Parameters["id"]);
            Windows.RunApplication("Content Editor", parameters.ToString());
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

            IMessage iMessage;
            while (job.MessageQueue.GetMessage(out iMessage))
            {
                iMessage.Execute();
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
    }
}