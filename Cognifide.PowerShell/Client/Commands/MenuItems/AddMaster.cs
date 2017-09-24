using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    public class AddMaster : Sitecore.Shell.Framework.Commands.AddMaster
    {
        public override void Execute(CommandContext context)
        {
            if (context.Items.Length != 1 || !context.Items[0].Access.CanCreate())
                return;
            Item item = context.Items[0];
            var scriptLibDatabase = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb);
            var master = context.Parameters["master"];
            var scriptItem = scriptLibDatabase.GetItem(master);
            SheerResponse.Timer(
                scriptItem.IsPowerShellScript()
                    ? $"item:executescript(id={item.ID},db={item.Database.Name},script={scriptItem.ID},scriptDb={scriptItem.Database.Name})"
                    : $"item:addmaster:spefallback(master={master},id={item.ID})",
                10);
        }
    }
}