using System;
using System.Collections.Generic;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class ScriptLibraryMenuItem : Command
    {
        public string IntegrationPoint { get; protected set; }

        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }

        public void SetupIntegrationPoint(CommandContext context)
        {
            Assert.IsNotNull(context, "Context is null.");
            IntegrationPoint = !string.IsNullOrEmpty(context.Parameters["integrationPoint"])
                ? context.Parameters["integrationPoint"]
                : string.Empty;
        }

        public override void Execute(CommandContext context)
        {
            SheerResponse.DisableOutput();
            var subMenu = new ContextMenu();
            var menuItems = new List<Control>();
            var menuItemId = context.Parameters["menuItemId"];

            if (string.IsNullOrEmpty(menuItemId))
            {
                // a bit of a hacky way to determine the caller so we can display the menu
                // in proximity to the triggering control
                var parameters = new UrlString("?" + Context.Items["SC_FORM"]);
                menuItemId = parameters.Parameters["__EVENTTARGET"];
            }

            SetupIntegrationPoint(context);
            var contextItem = context.Items.Length == 1
                ? context.Items[0]
                : string.IsNullOrEmpty(context.Parameters["db"]) || string.IsNullOrEmpty(context.Parameters["id"])
                    ? null
                    : Database.GetDatabase(context.Parameters["db"]).GetItem(new ID(context.Parameters["id"]));
            if (string.IsNullOrEmpty(IntegrationPoint))
            {
                var submenu =
                    Factory.GetDatabase(context.Parameters["scriptDB"]).GetItem(context.Parameters["scriptPath"]);
                GetLibraryMenuItems(contextItem, menuItems, submenu);
            }
            else
            {
                foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoint))
                {
                    GetLibraryMenuItems(contextItem, menuItems, root);
                }
            }

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
            SheerResponse.ShowContextMenu(menuItemId, "right", subMenu);
        }

        public override Control[] GetSubmenuItems(CommandContext context)
        {
            if (context.Items.Length != 1)
                return null;

            SetupIntegrationPoint(context);

            var menuItems = new List<Control>();

            foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.ContentEditorContextMenuFeature))
            {
                GetLibraryMenuItems(context.Items[0], menuItems, root);
            }

            return menuItems.ToArray();
        }

        internal static void GetLibraryMenuItems(Item contextItem, List<Control> menuItems, Item parent)
        {
            if (parent == null)
            {
                return;
            }
            foreach (Item scriptItem in parent.Children)
            {
                if (!RulesUtils.EvaluateRules(scriptItem["ShowRule"], contextItem))
                {
                    continue;
                }

                var menuItem = new MenuItem
                {
                    Header = scriptItem.DisplayName,
                    Icon = scriptItem.Appearance.Icon,
                    ID = scriptItem.ID.ToShortID().ToString(),
                    Disabled = !RulesUtils.EvaluateRules(scriptItem["EnableRule"], contextItem)
                };

                if (scriptItem.TemplateName == "PowerShell Script")
                {
                    if (contextItem != null)
                    {
                        menuItem.Click = string.Format("item:executescript(id={0},db={1},script={2},scriptDb={3})",
                            contextItem.ID, contextItem.Database.Name, scriptItem.ID, scriptItem.Database.Name);
                    }
                    else
                    {
                        menuItem.Click = string.Format("item:executescript(script={0},scriptDb={1})",
                            scriptItem.ID, scriptItem.Database.Name);
                    }
                }
                else
                {
                    menuItem.Type = MenuItemType.Submenu;
                    if (contextItem != null)
                    {
                        menuItem.Click = string.Format(
                            "item:scriptlibrary(id={0},db={1},scriptPath={2},scriptDB={3},menuItemId={4})",
                            contextItem.ID, contextItem.Database.Name, scriptItem.Paths.Path, scriptItem.Database.Name,
                            menuItem.ID);
                    }
                    else
                    {
                        menuItem.Click = string.Format(
                            "item:scriptlibrary(scriptPath={0},scriptDB={1},menuItemId={2})",
                            scriptItem.Paths.Path, scriptItem.Database.Name, menuItem.ID);
                    }
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