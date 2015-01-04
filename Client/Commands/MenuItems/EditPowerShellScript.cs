using System;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class EditPowerShellScript : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            if (context.Items.Length != 1)
                return CommandState.Hidden;

            if (context.Items[0].Template.Name.Equals("PowerShell Script", StringComparison.OrdinalIgnoreCase))
            {
                return CommandState.Enabled;
            }
            return CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            string itemId = context.Items[0].ID.ToString();
            string itemDb = context.Items[0].Database.Name;
            Item item = Factory.GetDatabase(itemDb).GetItem(new ID(itemId));

            var urlString = new UrlString();
            urlString.Append("id", item.ID.ToString());
            urlString.Append("db", itemDb);
            if (!string.IsNullOrEmpty(context.Parameters["frameName"]))
            {
                urlString.Add("pfn", context.Parameters["frameName"]);
            }
            Windows.RunApplication("PowerShell/PowerShellIse", urlString.ToString());
        }
    }
}