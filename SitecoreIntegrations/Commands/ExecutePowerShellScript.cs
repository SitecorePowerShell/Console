using System;
using Cognifide.PowerShell.Shell.Host;
using Cognifide.PowerShell.Shell.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class ExecutePowerShellScript : Command
    {
        public override void Execute(CommandContext context)
        {
            string scriptId = context.Parameters["script"];
            string scriptDb = context.Parameters["scriptDb"];
            Item scriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));

            string warning = scriptItem[ScriptItemFieldNames.PreExecutionWarning];
            string showResults = scriptItem[ScriptItemFieldNames.ShowResults];
            string itemId = context.Items[0].ID.ToString();
            string itemDb = context.Items[0].Database.Name;

            if (String.IsNullOrEmpty(warning))
            {
                ExecuteScript(itemId, itemDb, scriptId, scriptDb, showResults);
            }
            else
            {
                var clientArgs = new ClientPipelineArgs();
                clientArgs.Parameters.Add("script", scriptId);
                clientArgs.Parameters.Add("scriptDB", scriptDb);
                clientArgs.Parameters.Add("item", itemId);
                clientArgs.Parameters.Add("itemDB", context.Items[0].Database.Name);
                clientArgs.Parameters.Add("showResults", showResults);
                clientArgs.Parameters.Add("warning", warning);
                Context.ClientPage.Start(this, "ConfirmScriptExecution", clientArgs);
            }
        }

        public void ConfirmScriptExecution(ClientPipelineArgs args)
        {
            if (args.IsPostBack)
            {
                if (args.Result == "yes")
                {
                    ExecuteScript(args.Parameters["item"], args.Parameters["itemDB"], args.Parameters["script"],
                                  args.Parameters["scriptDB"], args.Parameters["showResults"]);
                }
            }
            else
            {
                Context.ClientPage.ClientResponse.Confirm(args.Parameters["warning"]);
                args.WaitForPostBack();
            }
        }

        public static void ExecuteScript(string itemId, string itemDb, string scriptId, string scriptDb,
                                         string showResults)
        {
            if (showResults == "1")
            {
                var str = new UrlString(UIUtil.GetUri("control:PowerShellResults"));
                str.Append("id", itemId);
                str.Append("db", itemDb);
                str.Append("scriptId", scriptId);
                str.Append("scriptDb", scriptDb);
                Context.ClientPage.ClientResponse.Broadcast(
                    SheerResponse.ShowModalDialog(str.ToString(), "800", "600", "PowerShell Script Results", false),
                    "Shell");
            }
            else
            {
                Item item = Factory.GetDatabase(itemDb).GetItem(new ID(itemId));
                Item scriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));
                ScriptSession scriptSession = null;

                try
                {
                    string persistentSessionId = scriptItem[ScriptItemFieldNames.PersistentSessionId];
                    scriptSession =
                        ScriptSessionManager.GetSession(persistentSessionId, ApplicationNames.Context, true);
                    scriptSession.Initialize(!string.IsNullOrEmpty(persistentSessionId));
                    scriptSession.ExecuteScriptPart("Set-HostProperty -HostWidth 80");
                    scriptSession.ExecuteScriptPart(scriptSession.Settings.Prescript);
                    scriptSession.ExecuteScriptPart(
                        String.Format(
                            "cd \"{0}:{1}\"", item.Database.Name, item.Paths.Path.Replace("/", "\\").Substring(9)));
                    scriptSession.ExecuteScriptPart(scriptItem[ScriptItemFieldNames.Script]);
                }
                finally
                {
                    if (string.IsNullOrEmpty(scriptItem[ScriptItemFieldNames.PersistentSessionId]))
                    {
                        scriptSession.Dispose();
                    }
                }
            }
        }
    }
}