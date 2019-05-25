using System.Collections.Generic;
using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Modules;
using Spe.Core.Settings.Authorization;

namespace Spe.Client.Commands.MenuItems
{
    public class ItemNew : Sitecore.Shell.Framework.Commands.ItemNew
    {
        public override Control[] GetSubmenuItems(CommandContext context)
        {
            var controls = base.GetSubmenuItems(context);

            if (controls == null || context.Items.Length != 1 || context.Items[0] == null)
            {
                return controls;
            }
            if (!
                ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name))
            {
                return controls;
            }


            var menuItems = new List<Control>();

            var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.ContentEditorInsertItemFeature);

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.ContentEditorInsertItemFeature))
            {
                ScriptLibraryMenuItem.GetLibraryMenuItems(context.Items[0], menuItems, root);
            }

            if (roots.Count > 0 && controls.Length > 0)
            {
                menuItems.Add(new MenuDivider());
            }
            menuItems.AddRange(controls);
            return menuItems.ToArray();

        }
    }
}