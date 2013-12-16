using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Sites;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet("Get", "Rendering")]
    [OutputType(new[] {typeof (RenderingReference)}, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetRenderingCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Mandatory = true, ParameterSetName = "Item from Pipeline")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        public string Id { get; set; }

        [Parameter]
        public string Device { get; set; }

        [Parameter]
        public string PlaceholderPath { get; set; }

        protected override void ProcessRecord()
        {
            Item = FindItemFromParameters(Item, Path, Id);
            LayoutDefinition layout =
              LayoutDefinition.Parse(Item[Sitecore.FieldIDs.LayoutField]);
/*
            var device = CurrentDatabase.Resources.Devices.GetAll().FirstOrDefault(d => string.Equals(d.Name, Device, StringComparison.OrdinalIgnoreCase));
            if (device == null)
            {
                device = CurrentDatabase.Resources.Devices.GetAll().FirstOrDefault(d => d.IsDefault );
            }
            WriteObject(Item.Visualization.GetRenderings(device, false));
*/
        }
    }
}