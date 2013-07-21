using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Alert")]
    public class ShowAlertCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, Mandatory = true)]
        public string Title { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    JobContext.Alert(Title);
                });
        }
    }
}