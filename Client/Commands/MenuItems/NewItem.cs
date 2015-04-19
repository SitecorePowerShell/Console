using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Core.Modules;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
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