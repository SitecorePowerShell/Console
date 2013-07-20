using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class ScriptLibraryMenuItem : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return PowerShellUiUserOptions.ShowContextMenuScripts ? CommandState.Enabled : CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            SheerResponse.DisableOutput();
            var subMenu = new Menu();
            var menuItems = new List<Control>();
            string menuItemId = context.Parameters["menuItemId"];
            Item contextItem = context.Items.Length == 1
                                   ? context.Items[0]
                                   : Context.Database.GetItem(new ID(context.Parameters["id"]));
            GetLibraryMenuItems(contextItem, menuItems, context.Parameters["scriptDB"], context.Parameters["scriptPath"]);

            foreach (Control item in menuItems)
            {
                var menuItem = item as MenuItem;
                if (menuItem != null)
                {
                    subMenu.Add(menuItem.ID, menuItem.Header, menuItem.Icon, menuItem.Hotkey, menuItem.Click,
                                menuItem.Checked, menuItem.Radiogroup, menuItem.Type);
                }
            }
            SheerResponse.EnableOutput();
            SheerResponse.ShowPopup(menuItemId, "right", subMenu);
        }

        public override Control[] GetSubmenuItems(CommandContext context)
        {
            if (context.Items.Length != 1)
                return null;

            const string scriptLibPath =
                "/sitecore/system/Modules/PowerShell/Script Library/Content Editor Context Menu";
            var menuItems = new List<Control>();

            GetLibraryMenuItems(context.Items[0], menuItems, "core", scriptLibPath);
            GetLibraryMenuItems(context.Items[0], menuItems, "master", scriptLibPath);

            return menuItems.ToArray();
        }

        private static void GetLibraryMenuItems(Item contextItem, List<Control> menuItems, string scriptDb,
                                                string scriptLibPath)
        {
            Item parent = Factory.GetDatabase(scriptDb).GetItem(scriptLibPath);
            foreach (Item scriptItem in parent.Children)
            {
                string allowedBranchesStr = scriptItem["AllowInBranches"];
                if (!string.IsNullOrEmpty(allowedBranchesStr))
                {
                    MultilistField multiselectField = scriptItem.Fields["AllowInBranches"];
                    if (multiselectField != null)
                    {
                        Item[] allowedBranches = multiselectField.GetItems();
                        var found = false;
                        for (int i = 0; !found && i < allowedBranches.Length; i++)
                        {
                            if (contextItem.ID == allowedBranches[i].ID ||
                                contextItem.Axes.IsDescendantOf(allowedBranches[i]))
                            {
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            continue;
                        }
                    }
                }
                string allowedTemplatesStr = scriptItem["AllowOnTemplates"];
                var baseTemplates = contextItem.Template.BaseTemplates.Select(t => t.ID).ToList();
                baseTemplates.Add(contextItem.TemplateID);
                if (!string.IsNullOrEmpty(allowedTemplatesStr))
                {
                    MultilistField multiselectField = scriptItem.Fields["AllowOnTemplates"];
                    if (multiselectField != null)
                    {
                        var allowedTemplates = multiselectField.TargetIDs;
                        var found = false;
                        for (int i = 0; !found && i < allowedTemplates.Length; i++)
                        {
                            if (baseTemplates.Contains(allowedTemplates[i]))
                            {
                                found = true;
                            }
                        }
                        if (!found)
                        {
                            continue;
                        }
                    }
                }
                var menuItem = new MenuItem
                    {
                        Header = scriptItem.DisplayName,
                        Icon = scriptItem.Appearance.Icon,
                        ID = scriptItem.ID.ToShortID().ToString()
                    };

                if (scriptItem.TemplateName == "PowerShell Script")
                {
                    menuItem.Click = string.Format("item:executescript(id={0},script={1},scriptDb={2})",
                                                   contextItem.ID, scriptItem.ID, scriptItem.Database.Name);
                }
                else
                {
                    menuItem.Type = MenuItemType.Submenu;
                    menuItem.Click = string.Format(
                        "item:scriptlibrary(id={0},scriptPath={1},scriptDB={2},menuItemId={3})",
                        contextItem.ID, scriptItem.Paths.Path, scriptItem.Database.Name, menuItem.ID);
                }
                menuItems.Add(menuItem);
            }
        }

        public override string GetClick(CommandContext context, string click)
        {
            return string.Empty;
        }
    }
}