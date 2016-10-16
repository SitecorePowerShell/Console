using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings.Authorization;
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
            return ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name,
                false)
                ? CommandState.Enabled
                : CommandState.Hidden;
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
                if (menuItem == null) continue;

                var subItem = subMenu.Add(menuItem.ID, menuItem.Header, menuItem.Icon, menuItem.Hotkey,
                    menuItem.Click,
                    menuItem.Checked, menuItem.Radiogroup, menuItem.Type);
                subItem.Disabled = menuItem.Disabled;
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

            if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name,
                false))
            {
                return;
            }


            var scriptTemplateId = new ID("{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}");
            var scriptLibaryTemplateId = new ID("{AB154D3D-1126-4AB4-AC21-8B86E6BD70EA}");
            foreach (var scriptItem in parent.Children.Where(p => p.TemplateID == scriptTemplateId || p.TemplateID == scriptLibaryTemplateId))
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

                if (scriptItem.IsPowerShellScript())
                {
                    menuItem.Click = contextItem != null ? $"item:executescript(id={contextItem.ID},db={contextItem.Database.Name},script={scriptItem.ID},scriptDb={scriptItem.Database.Name})" : $"item:executescript(script={scriptItem.ID},scriptDb={scriptItem.Database.Name})";
                }
                else
                {
                    menuItem.Type = MenuItemType.Submenu;
                    menuItem.Click = contextItem != null ? $"item:scriptlibrary(id={contextItem.ID},db={contextItem.Database.Name},scriptPath={scriptItem.Paths.Path},scriptDB={scriptItem.Database.Name},menuItemId={menuItem.ID})" : $"item:scriptlibrary(scriptPath={scriptItem.Paths.Path},scriptDB={scriptItem.Database.Name},menuItemId={menuItem.ID})";
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