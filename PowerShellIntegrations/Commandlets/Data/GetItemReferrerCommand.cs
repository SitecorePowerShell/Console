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
    [OutputType(new[] { typeof(Item) }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetItemReferrerCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Position = 0)]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public string Id { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        public Language Language { get; set; }

        protected override void ProcessRecord()
        {
            Item linkedItem = FindItemFromParameters(Item, Path, Id, Language);
            var linkDb = Sitecore.Globals.LinkDatabase;
            if (linkDb.GetReferrerCount(linkedItem) > 0)
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