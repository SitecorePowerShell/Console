using System;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.Client.Commands
{
    [Serializable]
    public class GalleryRuntimeQueryState : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return context.Parameters["ScriptRunning"] == "1" ? CommandState.Disabled : CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            //dummy
        }

        public override string GetClick(CommandContext context, string click)
        {
            return string.Empty;
        }
    }
}