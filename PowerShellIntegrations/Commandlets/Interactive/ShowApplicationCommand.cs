using System.Collections;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Application")]
    public class ShowApplicationCommand : BaseFormCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Application { get; set; }

/*
        [Parameter]
        public string Url { get; set; }
*/
        [Parameter(Position = 1)]
        public Hashtable Parameter{ get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public SwitchParameter Modal { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public object Data { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                        var message =
                            new ShowApplicationMessage(Application, Title, Icon, WidthString, HeightString, Modal.IsPresent, Parameter);

                        JobContext.MessageQueue.PutMessage(message);
                        JobContext.Flush();
                });
        }
    }
}