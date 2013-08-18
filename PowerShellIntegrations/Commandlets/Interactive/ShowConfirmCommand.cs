using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Confirm")]
    [OutputType(new[] { typeof(string) })]
    public class ShowConfirmCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Title { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    string response = JobContext.Confirm(Title);
                    WriteObject(response);
                });
        }
    }
}