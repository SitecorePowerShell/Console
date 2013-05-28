using System;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    [Cmdlet("Get", "AnalyticsView", DefaultParameterSetName = "Name")]
    public class GetAnalyticsViewCommand : AnalyticsBaseCommand
    {
        [Parameter(ValueFromPipeline = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                Guid itemId = Item.ID.ToGuid();
                foreach (Pages page in Context.Pages.Where(p => p.ItemId == itemId))
                {
                    WriteObject(page);
                }
            }
            else
            {
                foreach (Pages page in Context.Pages)
                {
                    WriteObject(page);
                }
            }
        }
    }
}