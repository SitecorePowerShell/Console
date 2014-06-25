using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Sites;
using Sitecore.Workflows;
using Sitecore.Workflows.Simple;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    //[Cmdlet("Get", "Layout")]
    [OutputType(new[] {typeof (RenderingReference)}, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class GetLayoutCommand : BaseItemCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            var layout = LayoutDefinition.Parse(item[FieldIDs.LayoutField]);

            for (int i = 0; i < layout.Devices.Count; i++)
            {
                DeviceDefinition device = layout.Devices[i] as DeviceDefinition;
/*
                RenderingReference refs = new RenderingReference();
                RenderingItem item = new RenderingItem();
*/
                WriteObject(device, false);
            }
        }
    }
}