using System.Management.Automation;
using Spe.Commands.Interactive.Messages;

namespace Spe.Commands.Interactive
{
    [Cmdlet(VerbsLifecycle.Invoke, "JavaScript")]
    public class InvokeJavaScriptCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Script { get; set; }

        [Parameter]
        public SwitchParameter OnScriptEnd { get; set; }

        protected override void ProcessRecord()
        {
            if (OnScriptEnd)
            {
                HostData.DeferredMessages.Add($"script:{Script}");
                return;
            }

            if (!CheckSessionCanDoInteractiveAction())
                return;
            LogErrors(() => PutMessage(new InvokeJavaScriptMessage(Script)));
        }
    }
}