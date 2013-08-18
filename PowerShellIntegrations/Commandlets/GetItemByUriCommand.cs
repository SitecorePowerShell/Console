using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Get", "ItemByUri", DefaultParameterSetName = "Item")]
    [OutputType(new[] { typeof(Item) })]
    public class GetItemByUriCommand : BaseCommand
    {
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0)]
        public string ItemUri { get; set; }

        protected override void ProcessRecord()
        {
            if (!string.IsNullOrEmpty(ItemUri))
            {
                ItemUri uri = Sitecore.Data.ItemUri.Parse(ItemUri);
                Item item = Factory.GetDatabase(uri.DatabaseName).GetItem(uri.ItemID, uri.Language, uri.Version);
                PSObject psobj = ItemShellExtensions.GetPsObject(SessionState, item);
                WriteObject(psobj, false);
            }
        }
    }
}