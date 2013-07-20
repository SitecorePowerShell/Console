using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class ToggleContextTerminal : ToggleContentEditorIntegrationBase
    {
        public override CommandState QueryState(CommandContext context)
        {
            if (PowerShellUiUserOptions.ShowContextMenuTerminal)
            {
                return CommandState.Down;
            }
            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            PowerShellUiUserOptions.ShowContextMenuTerminal = !PowerShellUiUserOptions.ShowContextMenuTerminal;
            RefreshRibbon(context, "RibbonTogglePSConsole", PowerShellUiUserOptions.ShowContextMenuTerminal);
        }
    }
}