using Cognifide.PowerShell.PowerShellIntegrations;
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
                return ScriptLibrary.Path+"Content Editor/Context Menu";
            }
        }

        public override Control[] GetSubmenuItems(CommandContext context)
        {
            return base.GetSubmenuItems(context);
        }
    }
}