using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Web.Script.Services;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.PowerShellIntegrations.Host;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ListView")]
    [OutputType(new[] {typeof (string)})]
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
        public SwitchParameter ActionsInSession { get; set; }

        protected override void EndProcessing()
        {
            base.EndProcessing();
            LogErrors(() =>
            {
                int pageSize = PageSize == 0 ? 25 : PageSize;
                if (Data != null)
                {
                    PutMessage(new ShowListViewMessage(cumulativeData, pageSize, Title ?? "PowerShell Script Results",
                        Icon, WidthString, HeightString, Modal.IsPresent, InfoTitle, InfoDescription,
                        ActionsInSession ? HostData.SessionId : "",
                        ActionData, Property, ViewName));
                    FlushMessages();
                }
            });
        }
    }
}