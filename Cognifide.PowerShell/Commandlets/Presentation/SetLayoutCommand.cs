using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Layout", SupportsShouldProcess = true)]
    public class SetLayoutCommand : BaseLayoutCommand
    {
        [Parameter(Mandatory = true)]
        public DeviceItem Device { get; set; }

        [Parameter]
        public Item Layout { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), string.Format("Set layout '{0}'", Layout.GetProviderPath())))
            {
                LayoutField layoutField = item.Fields[LayoutFieldId];
                if (layoutField == null)
                {
                    return;
                }

                var layout = LayoutDefinition.Parse(layoutField.Value);

                var device = layout.GetDevice(Device.ID.ToString());

                if (string.Equals(device.Layout, Layout.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    //same layout as already set - no point in setting it again
                    return;
                }

                device.Layout = Layout.ID.ToString();

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    layoutField.Value = outputXml;
                });
            }
        }
    }
}