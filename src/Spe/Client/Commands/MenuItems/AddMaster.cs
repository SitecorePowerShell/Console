using Sitecore.Configuration;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Extensions;
using Spe.Core.Settings;

namespace Spe.Client.Commands.MenuItems
{
    public class AddMaster : Sitecore.Shell.Framework.Commands.AddMaster
    {
        public override void Execute(CommandContext context)
        {
            if (context.Items.Length != 1 || !context.Items[0].Access.CanCreate())
                return;
            var item = context.Items[0];
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