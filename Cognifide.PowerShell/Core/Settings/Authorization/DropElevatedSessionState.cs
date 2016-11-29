using System;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

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
            SessionElevationManager.DropSessionTokenElevation(SessionElevationManager.ItemSave);
            Context.ClientPage.SendMessage(this, $"item:refresh(id={context.Items[0]})");
        }
    }
}