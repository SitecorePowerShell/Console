using System.Collections;
using System.Management.Automation;
using Spe.Commandlets.Interactive.Messages;

namespace Spe.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Application")]
    public class ShowApplicationCommand : BaseFormCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Application { get; set; }

        [Parameter(Position = 1)]
        public Hashtable Parameter { get; set; }

        [Parameter]
        public SwitchParameter Modal { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                var message =
                    new ShowApplicationMessage(Application, Title, Icon, WidthString, HeightString, Modal.IsPresent,
                        Parameter);

                PutMessage(message);
            });
        }
    }
}