using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Cognifide.PowerShell.Console.Services;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using NVelocity;
using NVelocity.App;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellVelociyOutput : BaseForm //, IPowerShellRunner
    {
        protected VelocityContext velocityContext;
        protected Literal velocityOutput;

        protected override void OnLoad(EventArgs e)
        {
            Velocity.Init();
            velocityContext = new VelocityContext();
            Item currentItem;
            Item scriptItem = null;
            var scriptId = WebUtil.SafeEncode(WebUtil.GetQueryString("scriptId"));
            var scriptDb = WebUtil.SafeEncode(WebUtil.GetQueryString("scriptDb", ApplicationSettings.ScriptLibraryDb));
            if (!string.IsNullOrEmpty(scriptId))
            {
                scriptItem = Factory.GetDatabase(scriptDb).GetItem(scriptId);
            }
            if (scriptItem == null && Sitecore.Context.Item.TemplateName == TemplateNames.ScriptTemplateName)
            {
                scriptItem = Sitecore.Context.Item;
            }
            if (scriptItem == null)
            {
                velocityOutput.Text = "Could not resolve PowerShell script or no script item specified.";
                return;
            }
            var script = scriptItem["script"];

            var id = WebUtil.SafeEncode(WebUtil.GetQueryString("id"));
            if (!string.IsNullOrEmpty(id))
            {
                var db =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("db",
                        WebUtil.GetQueryString("database", (Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database).Name)));
                var la =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("la",
                        WebUtil.GetQueryString("language", (Sitecore.Context.ContentDatabase ?? Sitecore.Context.Database).Name)));
                var vs =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("vs",
                        WebUtil.GetQueryString("version", Version.Latest.Number.ToString())));
                currentItem = Factory.GetDatabase(db)
                    .GetItem(new ID(id), LanguageManager.GetLanguage(la), new Version(vs));
            }
            else
            {
                currentItem = Sitecore.Context.Item;
            }

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                scriptSession.SetItemLocationContext(currentItem);
                scriptSession.ExecuteScriptPart(scriptSession.Settings.Prescript);
                scriptSession.ExecuteScriptPart(script);

                // add output
                var output = new List<string>();

                if (scriptSession.Output.Count > 0)
                {
                    output.Add(
                        scriptSession.Output
                            .Select(p => p.Terminated ? p.Text + "\n" : p.Text)
                            .Aggregate((current, next) => current + next)
                        );
                }
                velocityContext.Put("scriptOutput", output);

                // add variables
                List<string> variableNames = new List<string>();
                foreach (var variable in scriptSession.Variables)
                {
                    velocityContext.Put(variable.Name, variable.Value);
                    variableNames.Add(variable.Name);
                }

                velocityContext.Put("variableNames", variableNames);

                StringWriter result = new StringWriter();
                try
                {
                    Velocity.Evaluate(velocityContext, result, "PowerShell Script",
                        scriptItem["VelocityTemplate"]);
                }
                catch (NVelocity.Exception.ParseErrorException ex)
                {
                    Log.Error(string.Format("Error parsing template for the {0} item \n {1}",
                        currentItem.Paths.Path, ex.ToString()), this);
                }
                velocityOutput.Text = result.GetStringBuilder().ToString();
            }
            //litResult.
        }

        public bool MonitorActive
        {
            set { throw new NotImplementedException(); }
        }
    }
}