using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class AbortQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] == "1" && context.Parameters["inBreakpoint"] != "1"
                ? CommandState.Enabled
                : CommandState.Disabled;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
        }
    }
}