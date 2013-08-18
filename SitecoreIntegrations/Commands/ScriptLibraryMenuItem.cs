using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Rules;
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
                if (!EvaluateRules(scriptItem["ShowRule"], scriptItem.Database, contextItem))
                {
                    continue;
                }

                var menuItem = new MenuItem
                {
                    Header = scriptItem.DisplayName,
                    Icon = scriptItem.Appearance.Icon,
                    ID = scriptItem.ID.ToShortID().ToString(),
                };
                menuItem.Disabled = !EvaluateRules(scriptItem["EnableRule"], scriptItem.Database, contextItem);

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

        public static bool EvaluateRules(string strRules, Database database, Item contextItem)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            var rules = RuleFactory.ParseRules<RuleContext>(database, strRules);
            var ruleContext = new RuleContext
            {
                Item = contextItem
            };

            return rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }
    }
}