using System;
using System.Globalization;
using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    [Serializable]
    public class ExecutePowerShellScript : Command
    {
        public override void Execute(CommandContext context)
        {
            string scriptId = context.Parameters["script"];
            string scriptDb = context.Parameters["scriptDb"];

            string itemId = string.Empty;
            string itemDb = string.Empty;
            string itemLang = string.Empty;
            string itemVer = string.Empty;

            if (context.Items.Length > 0)
            {
                itemId = context.Items[0].ID.ToString();
                itemDb = context.Items[0].Database.Name;
                itemLang = context.Items[0].Language.Name;
                itemVer = context.Items[0].Version.Number.ToString(CultureInfo.InvariantCulture);
            }

            var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            str.Append("id", itemId);
            str.Append("db", itemDb);
            str.Append("lang", itemLang);
            str.Append("ver", itemVer);
            str.Append("scriptId", scriptId);
            str.Append("scriptDb", scriptDb);
            Context.ClientPage.ClientResponse.Broadcast(
                SheerResponse.ShowModalDialog(str.ToString(), "400", "260", "PowerShell Script Results", false),
                "Shell");
        }


        public override CommandState QueryState(CommandContext context)
        {
/*
            if (context.Items.Length == 1)
            {
            if (!EvaluateRules(scriptItem["ShowRule"], context.Items[0]))
            {
                continue;
            }

            menuItem.Disabled = !EvaluateRules(scriptItem["EnableRule"], context.Items[0]);
            }
*/
            return base.QueryState(context);
        }
    }
}