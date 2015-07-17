using System;
using System.Globalization;
using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class ExecutePowerShellScript : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] != "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            var scriptId = context.Parameters["script"];
            var scriptDb = context.Parameters["scriptDb"];

            var itemId = string.Empty;
            var itemDb = string.Empty;
            var itemLang = string.Empty;
            var itemVer = string.Empty;

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
    }
}