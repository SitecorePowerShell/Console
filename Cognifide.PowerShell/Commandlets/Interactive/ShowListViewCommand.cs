using System.Collections.Generic;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ListView")]
    [OutputType(typeof (string))]
    public class ShowListViewCommand : BaseListViewCommand
    {

        [Parameter]
        public int PageSize { get; set; }

        [Parameter]
        public string Icon { get; set; }

        [Parameter]
        public string InfoTitle { get; set; }

        [Parameter]
        public string InfoDescription { get; set; }

        [Parameter]
        public SwitchParameter Modal { get; set; }

        [Parameter]
        public object ActionData { get; set; }

        [Parameter]
        public string ViewName { get; set; }

        [Parameter]
        public string MissingDataMessage { get; set; }

        [Parameter]
        public SwitchParameter ActionsInSession { get; set; }

        [Parameter]
        public ShowListViewFeatures Show { get; set; } = ShowListViewFeatures.All;

        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                var pageSize = PageSize == 0 ? 25 : PageSize;
                if (Data == null)
                {
                    Data = new List<object>();
                }
                if (Data != null)
                {
                    PutMessage(new ShowListViewMessage(CumulativeData, pageSize, Title ?? "PowerShell Script Results",
                        Icon, WidthString, HeightString, Modal.IsPresent, InfoTitle, InfoDescription,
                        ActionsInSession ? HostData.SessionId : "",
                        ActionData, ProcessedProperty, ViewName, MissingDataMessage, Show));
                }
            });
        }
    }
}