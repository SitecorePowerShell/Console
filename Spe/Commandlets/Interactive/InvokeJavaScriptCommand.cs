using System.Management.Automation;
using Spe.Commandlets.Interactive.Messages;

namespace Spe.Commandlets.Interactive
{
    [Cmdlet(VerbsLifecycle.Invoke, "JavaScript")]
    public class InvokeJavaScriptCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Script { get; set; }

        protected override void ProcessRecord()
        {
            if (!CheckSessionCanDoInteractiveAction()) return;

            LogErrors(() => PutMessage(new InvokeJavaScriptMessage(Script)));
        }
    }
}