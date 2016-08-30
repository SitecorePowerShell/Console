using System;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
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
            var itemId = context.Items[0].ID.ToString();
            var itemDb = context.Items[0].Database.Name;
            var item = Factory.GetDatabase(itemDb).GetItem(new ID(itemId));

            var urlString = new UrlString();
            urlString.Append("item", item.Paths.Path.ToLower().Replace("sitecore/", ""));
            urlString.Append("db", itemDb);
            Sitecore.Shell.Framework.Windows.RunApplication("PowerShell/PowerShell Console", urlString.ToString());
        }

        public override CommandState QueryState(CommandContext context)
        {
            var user = Context.User;
            if (!user.IsAdministrator &&
                !user.IsInRole(Role.FromName("sitecore\\Sitecore Client Developing")))
            {
                return CommandState.Hidden;
            }
            return CommandState.Enabled;
        }
    }
}