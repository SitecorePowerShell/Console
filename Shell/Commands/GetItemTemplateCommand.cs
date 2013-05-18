using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Shell.Commands
{
    [Cmdlet("Get", "ItemTemplate", SupportsShouldProcess = true, DefaultParameterSetName = "Item")]
    public class GetItemTemplateCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessRecord()
        {
            WriteObject(Item.Template, true);
            if (Recurse.IsPresent)
            {
                WriteObject(Item.Template.BaseTemplates, true);
            }
        }
    }
}