using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using NVelocity;
using NVelocity.App;
using NVelocity.Exception;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Client.Applications
{
    public class PowerShellVelociyOutput : BaseForm //, IPowerShellRunner
    {
        protected VelocityContext VelocityContext;
        protected Literal VelocityOutput;

        protected override void OnLoad(EventArgs e)
        {
            Velocity.Init();
            VelocityContext = new VelocityContext();
            Item currentItem;
            Item scriptItem = null;
            var scriptId = WebUtil.SafeEncode(WebUtil.GetQueryString("scriptId"));
            var scriptDb = WebUtil.SafeEncode(WebUtil.GetQueryString("scriptDb", ApplicationSettings.ScriptLibraryDb));
            if (!string.IsNullOrEmpty(scriptId))
            {
                scriptItem = Factory.GetDatabase(scriptDb).GetItem(scriptId);
            }

            if (scriptItem == null)
            {
                if (Context.Item.IsPowerShellScript())
                {
                    scriptItem = Context.Item;
                }
                else
                {
                    VelocityOutput.Text = "Could not resolve PowerShell script or no script item specified.";
                    return;
                }
            }

            var script = scriptItem["script"];
            
            var id = WebUtil.SafeEncode(WebUtil.GetQueryString("id"));
            if (!string.IsNullOrEmpty(id))
            {
                var db =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("db",
                        WebUtil.GetQueryString("database", (Context.ContentDatabase ?? Context.Database).Name)));
                var la =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("la",
                        WebUtil.GetQueryString("language", (Context.ContentDatabase ?? Context.Database).Name)));
                var vs =
                    WebUtil.SafeEncode(WebUtil.GetQueryString("vs",
                        WebUtil.GetQueryString("version", Version.Latest.Number.ToString())));
#pragma warning disable 618
                currentItem = Factory.GetDatabase(db)
                    .GetItem(new ID(id), LanguageManager.GetLanguage(la), new Version(vs));
#pragma warning restore 618
            }
            else
            {
                currentItem = Context.Item;
            }

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                scriptSession.SetItemLocationContext(currentItem);
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
                VelocityContext.Put("scriptOutput", output);

                // add variables
                List<string> variableNames = new List<string>();
                foreach (var variable in scriptSession.Variables)
                {
                    VelocityContext.Put(variable.Name, variable.Value);
                    variableNames.Add(variable.Name);
                }

                VelocityContext.Put("variableNames", variableNames);

                StringWriter result = new StringWriter();
                try
                {
                    Velocity.Evaluate(VelocityContext, result, "PowerShell Script",
                        scriptItem["VelocityTemplate"]);
                }
                catch (ParseErrorException ex)
                {
                    PowerShellLog.Error($"Error parsing template for the {currentItem.Paths.Path} item.", ex);
                }
                VelocityOutput.Text = result.GetStringBuilder().ToString();
            }
            //litResult.
        }

        public bool MonitorActive
        {
            set { throw new NotImplementedException(); }
        }
    }
}