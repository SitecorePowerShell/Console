using System;
using System.Text;
using System.Web.UI.WebControls;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellResults : BaseForm
    {
        protected JobMonitor Monitor;
        protected Scrollbox All;
        protected Scrollbox Result;
        protected Scrollbox Promo;

        public string Script { get; set; }
        public ApplicationSettings Settings { get; set; }
        public bool NonInteractive { get; set; }

        protected Item ScriptItem { get; set; }
        protected Item CurrentItem { get; set; }

        protected Literal BackgroundColor;
        protected Literal ForegroundColor;

        public string PersistentId { get; set; }

        public string FlushOutputBuffer { get; set; }


        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            string itemId = WebUtil.GetQueryString("id");
            string itemDb = WebUtil.GetQueryString("db");
            string scriptId = WebUtil.GetQueryString("scriptId");
            string scriptDb = WebUtil.GetQueryString("scriptDb");

            ScriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));
            ScriptItem.Fields.ReadAll();
            if (!string.IsNullOrEmpty(itemId) && !string.IsNullOrEmpty(itemDb))
            {
                CurrentItem = Factory.GetDatabase(itemDb).GetItem(new ID(itemId));
            }
            Settings = ApplicationSettings.GetInstance(ApplicationNames.Context,false);
            PersistentId = ScriptItem[ScriptItemFieldNames.PersistentSessionId];
            FlushOutputBuffer = ScriptItem[ScriptItemFieldNames.PersistentSessionBufferFlush];
            var foregroundColor = OutputLine.ProcessHtmlColor(Settings.ForegroundColor);
            var backgroundColor= OutputLine.ProcessHtmlColor(Settings.BackgroundColor);

            All.Style.Add("color", foregroundColor);
            All.Style.Add("background-color", backgroundColor);
            Promo.Style.Add("color", foregroundColor);
            Promo.Style.Add("background-color", backgroundColor);
            Result.Style.Add("color", foregroundColor);
            Result.Style.Add("background-color", backgroundColor);


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

            if (!Context.ClientPage.IsEvent)
            {
                //this.
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
            string contextScript = string.Format("Set-HostProperty -HostWidth 80\n{0}\n{1}",
                                                 scriptSession.Settings.Prescript,
                                                 ScriptSession.GetDataContextSwitch(CurrentItem));

            var parameters = new object[]
                {
                    scriptSession,
                    contextScript,
                    ScriptItem[ScriptItemFieldNames.Script]
                };
            var runner = new ScriptRunner(ExecuteInternal, parameters);
            Monitor.Start("ScriptExecution", "PowerShellResults", runner.Run);
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
                var output = new StringBuilder(10240);
                if (scriptSession.Output != null)
                {
                    foreach (OutputLine outputLine in scriptSession.Output)
                    {
                        outputLine.GetHtmlLine(output);
                    }
                }
                if (Context.Job != null)
                {
                    Context.Job.Status.Result = string.Format("<pre>{0}</pre>", output);
                    JobContext.PostMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
            catch (Exception exc)
            {
                if (Context.Job != null)
                {
                    Context.Job.Status.Result =
                        string.Format("<pre style='background:red;'>{0}</pre>",
                                      scriptSession.GetExceptionString(exc));
                    JobContext.PostMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
        }

        [HandleMessage("psr:updateresults", true)]
        protected virtual void UpdateResults(ClientPipelineArgs args)
        {
            var result = JobManager.GetJob(Monitor.JobHandle).Status.Result as string;
            Context.ClientPage.ClientResponse.SetInnerHtml("Result", result ?? "Script finished - no results to display.");
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
            foreach (string key in message.Arguments.AllKeys)
            {
                context.Parameters.Add(key, message.Arguments[key]);
            }

            Dispatcher.Dispatch(message, context);
        }
    }
}