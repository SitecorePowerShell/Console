using Cognifide.PowerShell.PowerShellIntegrations;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    public class StartMenuScriptLibraryMenuItem : ScriptLibraryMenuItemBase
    {
        public override string ScriptLibraryPath
        {
            get
            {
                return ScriptLibrary.Path;
            }
        }
    }
}