using System;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class ListViewPagingQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["showPaging"] == "1"
                ? CommandState.Enabled
                : CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
        }
    }
}