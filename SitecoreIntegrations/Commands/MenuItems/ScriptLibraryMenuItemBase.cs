using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    [Serializable]
    public abstract class ScriptLibraryMenuItemBase : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return PowerShellUiUserOptions.ShowContextMenuScripts ? CommandState.Enabled : CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            SheerResponse.DisableOutput();
            var subMenu = new ContextMenu();
            var menuItems = new List<Control>();
            string menuItemId = context.Parameters["menuItemId"];
            Item contextItem = context.Items.Length == 1
                ? context.Items[0]
                : string.IsNullOrEmpty(context.Parameters["db"]) || string.IsNullOrEmpty(context.Parameters["id"])
                    ? null
                    : Database.GetDatabase(context.Parameters["db"]).GetItem(new ID(context.Parameters["id"]));
            GetLibraryMenuItems(contextItem, menuItems, context.Parameters["scriptDB"], context.Parameters["scriptPath"]);

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

            var menuItems = new List<Control>();

            GetLibraryMenuItems(context.Items[0], menuItems, "core", ScriptLibraryPath);
            GetLibraryMenuItems(context.Items[0], menuItems, "master", ScriptLibraryPath);

            return menuItems.ToArray();
        }

        public abstract string ScriptLibraryPath { get; }

        private static void GetLibraryMenuItems(Item contextItem, List<Control> menuItems, string scriptDb,
            string scriptLibPath)
        {
            Item parent = Factory.GetDatabase(scriptDb).GetItem(scriptLibPath);
            if (parent == null)
            {
                return;
            }
            foreach (Item scriptItem in parent.Children)
            {
                if (!EvaluateRules(scriptItem["ShowRule"], contextItem))
                {
                    continue;
                }

                var menuItem = new MenuItem
                {
                    Header = scriptItem.DisplayName,
                    Icon = scriptItem.Appearance.Icon,
                    ID = scriptItem.ID.ToShortID().ToString(),
                };
                menuItem.Disabled = !EvaluateRules(scriptItem["EnableRule"], contextItem);

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

        public static bool EvaluateRules(string strRules, Item contextItem)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase("master"), strRules);
            var ruleContext = new RuleContext
            {
                Item = contextItem
            };

            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }
    }
}