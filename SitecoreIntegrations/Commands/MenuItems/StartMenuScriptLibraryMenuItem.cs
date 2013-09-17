namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    public class StartMenuScriptLibraryMenuItem : ScriptLibraryMenuItemBase
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