using System;
using System.Collections.Generic;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class EditIseSettingsCombo : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] != "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            SheerResponse.DisableOutput();
            var subMenu = new ContextMenu();
            var menuItems = new List<Control>();
            var menuItemId = "iseSettingsDropdown"; //context.Parameters["Id"];

            if (string.IsNullOrEmpty(menuItemId))
            {
                // a bit of a hacky way to determine the caller so we can display the menu
                // in proximity to the triggering control
                var parameters = new UrlString("?" + Context.Items["SC_FORM"]);
                menuItemId = parameters.Parameters["__EVENTTARGET"];
            }

            var menuRootItem =
                Factory.GetDatabase("core")
                    .GetItem("/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Settings");
            GetMenuItems(menuItems, menuRootItem);

            foreach (var item in menuItems)
            {
                var menuItem = item as MenuItem;
                if (menuItem != null)
                {
                    var subItem = subMenu.Add(menuItem.ID, menuItem.Header, menuItem.Icon, menuItem.Hotkey,
                        menuItem.Click,
                        menuItem.Checked, menuItem.Radiogroup, menuItem.Type);
                    subItem.Disabled = menuItem.Disabled;
                }
            }
            SheerResponse.EnableOutput();
            subMenu.Visible = true;
            SheerResponse.ShowContextMenu(menuItemId, "down", subMenu);
        }

        private static void GetMenuItems(ICollection<Control> menuItems, Item parent)
        {
            if (parent == null)
            {
                return;
            }
            foreach (Item menuDataItem in parent.Children)
            {
                var menuItem = new MenuItem
                {
                    Header = menuDataItem.DisplayName,
                    Icon = menuDataItem.Appearance.Icon,
                    ID = menuDataItem.ID.ToShortID().ToString(),
                    Click = menuDataItem["Message"]
                };
                menuItems.Add(menuItem);
            }
        }
    }
}