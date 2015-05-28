using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class RuntimeQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] == "1" ? CommandState.Disabled : CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
            context.CustomData = "Result string";
        }
    }
}