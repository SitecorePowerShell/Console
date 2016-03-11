using System;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class ListViewFilterQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["showFilter"] == "1"
                ? CommandState.Enabled
                : CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
        }
    }
}