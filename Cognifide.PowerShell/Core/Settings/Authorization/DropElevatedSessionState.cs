using System;
using Sitecore;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Core.Settings.Authorization
{
    [Serializable]
    public class DropElevatedSessionState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            SessionElevationManager.DropSessionTokenElevation(ApplicationNames.ItemSave);
            Context.ClientPage.SendMessage(this, $"item:refresh(id={context.Items[0]})");
        }
    }
}