using System;
using Sitecore.Shell.Framework.Commands;

namespace Spe.Client.Commands
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