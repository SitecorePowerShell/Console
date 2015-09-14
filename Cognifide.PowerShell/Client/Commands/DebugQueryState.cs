using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class DebugQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["Debugging"] == "1" ? CommandState.Enabled : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
            context.CustomData = "Result string";
        }
    }
}