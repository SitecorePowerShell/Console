using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class ToggleContextMenu : ToggleContentEditorIntegrationBase
    {
 
        public override CommandState QueryState(CommandContext context)
        {
            if (PowerShellUiUserOptions.ShowContextMenuScripts)
            {
                return CommandState.Down;
            }
            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            PowerShellUiUserOptions.ShowContextMenuScripts = !PowerShellUiUserOptions.ShowContextMenuScripts;
            base.RefreshRibbon(context, "RibbonTogglePsScripts", PowerShellUiUserOptions.ShowContextMenuScripts);
        }
    }
}