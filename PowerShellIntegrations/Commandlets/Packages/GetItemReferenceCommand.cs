using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Install.Configuration;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("Get", "ItemReference", DefaultParameterSetName = "Item")]
    [OutputType(new[] { typeof(ExplicitItemSource) })]
    public class GetItemReferenceCommand : BasePackageCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(new ItemReference(Item.Uri, false).ToString());
        }
    }
}