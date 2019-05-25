using System.Management.Automation;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commandlets.Interactive.Messages;
using Spe.Core.VersionDecoupling;

namespace Spe.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Input")]
    [OutputType(typeof (string))]
    public class ShowInputCommand : BaseShellCommand
    {
        [Parameter(Position = 0, Mandatory = true)]
        public string Prompt { get; set; }

        [Parameter]
        public string DefaultValue { get; set; }

        [Parameter]
        public string Validation { get; set; }

        [Parameter]
        public string ErrorMessage { get; set; }

        [Parameter]
        public int MaxLength { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                if (!string.IsNullOrEmpty(Validation) || MaxLength > 0)
                {
                    PutMessage(new PromptMessage(Prompt, DefaultValue ?? "",
                        Validation ?? ".*",
                        ErrorMessage ?? "Invalid format",
                        MaxLength < 1 ? int.MaxValue : MaxLength));
                }
                else
                {
                    PutMessage(new PromptMessage(Prompt, DefaultValue ?? ""));
                }

                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager.GetContextJob();
                var alertresult = job.MessageQueue.GetResult() as string;
                WriteObject(alertresult);
            });
        }
    }
}