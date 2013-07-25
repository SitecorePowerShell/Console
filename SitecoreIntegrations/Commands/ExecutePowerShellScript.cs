using System;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Shell.Framework.Jobs;
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

            string showResults = scriptItem[ScriptItemFieldNames.ShowResults];
            string itemId = string.Empty;
            string itemDb = string.Empty;

            if (context.Items.Length > 0)
            {
                itemId = context.Items[0].ID.ToString();
                itemDb = context.Items[0].Database.Name;
            }

            var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            str.Append("id", itemId);
            str.Append("db", itemDb);
            str.Append("scriptId", scriptId);
            str.Append("scriptDb", scriptDb);
            str.Append("autoClose", showResults);
            Context.ClientPage.ClientResponse.Broadcast(
                SheerResponse.ShowModalDialog(str.ToString(), "400", "200", "PowerShell Script Results", false),
                "Shell");
        }
    }
}