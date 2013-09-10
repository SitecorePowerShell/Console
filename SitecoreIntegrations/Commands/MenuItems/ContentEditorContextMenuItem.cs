using Sitecore.Shell.Framework.Commands;
using Sitecore.Web.UI.HtmlControls;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    public class ContentEditorContextMenuItem : ScriptLibraryMenuItemBase
    {
        public override string ScriptLibraryPath
        {
            get
            {
                return "/sitecore/system/Modules/PowerShell/Script Library/Content Editor Context Menu";
            }
        }

        public override Control[] GetSubmenuItems(CommandContext context)
        {
            return base.GetSubmenuItems(context);
        }
    }
}