using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class DebugQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["inBreakpoint"] == "1"
                ? CommandState.Enabled
                : context.Parameters["debugging"] == "1"
                    ? CommandState.Disabled
                    : CommandState.Hidden;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
            context.CustomData = "Result string";
        }
    }
}