using System;
using System.Web;
using Cognifide.PowerShell.Client.Controls;
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
        public SpeJobMonitor Monitor { get; private set; }
        protected Literal Options;
        public ApplicationSettings Settings { get; set; }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context, false);
            HttpContext.Current.Response.AddHeader("X-UA-Compatible", "IE=edge");
            ApplicationSettings settings = ApplicationSettings.GetInstance(ApplicationNames.AjaxConsole, false);

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
        /// Handles the message.
        /// 
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
        }

    }
}