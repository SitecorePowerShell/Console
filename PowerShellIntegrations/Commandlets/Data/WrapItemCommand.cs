using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet("Wrap", "Item")]
    [OutputType(new[] {typeof (Item)})]
    public class WrapItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                WriteItem(Item);
            }
        }
    }
}