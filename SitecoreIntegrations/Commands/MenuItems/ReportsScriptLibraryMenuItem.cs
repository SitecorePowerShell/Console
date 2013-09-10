namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    public class ReportsScriptLibraryMenuItem : ScriptLibraryMenuItemBase
    {
        public override string ScriptLibraryPath
        {
            get
            {
                return "/sitecore/system/Modules/PowerShell/Script Library/";
            }
        }

    }
}