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
    public class GetItemReferenceCommand : BaseItemCommand
    {

        public SwitchParameter ItemLink { get; set; }

        protected override void ProcessItem(Item linkedItem)
        {
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