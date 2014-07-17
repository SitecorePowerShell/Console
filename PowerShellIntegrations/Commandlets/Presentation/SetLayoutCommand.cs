using System;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Layout")]
    [OutputType(new[] {typeof (RenderingReference)},
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class SetLayoutCommand : BaseItemCommand
    {
        [Parameter(Mandatory = true)]
        public DeviceItem Device { get; set; }

        [Parameter]
        public Item Layout { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessItem(Item item)
        {

            LayoutDefinition layout = LayoutDefinition.Parse(item[FieldIDs.LayoutField]);

            DeviceDefinition device = layout.GetDevice(Device.ID.ToString());

            if (string.Equals(device.Layout, Layout.ID.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                //same layout as already set - no point in setting it again
                return;
            }
            
            device.Layout = Layout.ID.ToString();

            item.Edit(p =>
            {
                string outputXml = layout.ToXml();
                Item["__Renderings"] = outputXml;
            });
        }
    }
}