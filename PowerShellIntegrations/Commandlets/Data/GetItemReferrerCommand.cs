using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Links;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet("Get", "ItemReferrer")]
    [OutputType(new[] {typeof (Item)}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"}
        )]
    public class GetItemReferrerCommand : BaseItemCommand
    {
        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Position = 0)]
        public SwitchParameter ItemLink { get; set; }

        protected override void ProcessItem(Item linkedItem)
        {
            var linkDb = Sitecore.Globals.LinkDatabase;
            if (linkDb.GetReferrerCount(linkedItem) > 0)
            {
                if (ItemLink)
                {
                    linkDb
                        .GetReferrers(linkedItem)
                        .ToList()
                        .ForEach(WriteObject);
                }
                else
                {
                    linkDb.GetReferrers(linkedItem)
                        .Select(link => link.GetSourceItem())
                        .Distinct(ItemEqualityComparer.Instance)
                        .ToList()
                        .ForEach(WriteItem);
                }
            }
        }
    }
}