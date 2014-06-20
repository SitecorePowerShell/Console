using System.Diagnostics;
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
    [Cmdlet(VerbsCommon.Get, "ItemReference")]
    [OutputType(new[] { typeof(Item), typeof(ItemLink) }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetItemReferenceCommand : BaseCommand
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

        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Position = 0)]
        public SwitchParameter ItemLink { get; set; }

        protected override void ProcessRecord()
        {
            Item linkedItem = FindItemFromParameters(Item, Path, Id, Language);
            var linkDb = Sitecore.Globals.LinkDatabase;
            if (linkDb.GetReferenceCount(linkedItem) > 0)
            {
                if (ItemLink)
                {
                    linkDb
                        .GetReferences(linkedItem)
                        .ToList()
                        .ForEach(WriteObject);
                }
                else
                {
                    linkDb.GetReferences(linkedItem)
                        .Select(link => link.GetSourceItem())
                        .Distinct(ItemEqualityComparer.Instance)
                        .ToList()
                        .ForEach(WriteItem);
                }
            }
        }
    }
}