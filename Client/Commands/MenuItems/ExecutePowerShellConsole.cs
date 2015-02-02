using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class ExecutePowerShellConsole : Command
    {
        public override void Execute(CommandContext context)
        {
            string itemId = context.Items[0].ID.ToString();
            string itemDb = context.Items[0].Database.Name;
            Item item = Factory.GetDatabase(itemDb).GetItem(new ID(itemId));

            var urlString = new UrlString();
            urlString.Append("item", item.Paths.Path.ToLower().Replace("sitecore/", ""));
            urlString.Append("db", itemDb);
            Windows.RunApplication("PowerShell/PowerShell Console", urlString.ToString());
        }

        public override CommandState QueryState(CommandContext context)
        {
            User user = Context.User;
            if (!user.IsAdministrator &&
                !user.IsInRole(Role.FromName("sitecore\\Sitecore Client Developing")))
            {
                return CommandState.Hidden;
            }
            return CommandState.Enabled;
        }
    }
}