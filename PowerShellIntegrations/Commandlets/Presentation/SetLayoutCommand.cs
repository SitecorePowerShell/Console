using System;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Layout")]
    public class SetLayoutCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(Mandatory = true)]
        public DeviceItem Device { get; set; }

        [Parameter]
        public Item Layout { get; set; }

        protected override void ProcessItem(Item item)
        {

            LayoutField layoutField = item.Fields[FieldIDs.LayoutField];
            if (layoutField == null || string.IsNullOrEmpty(layoutField.Value))
            {
                return;
            }

            LayoutDefinition layout = LayoutDefinition.Parse(layoutField.Value);

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