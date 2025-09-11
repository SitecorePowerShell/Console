using System;
using System.Data;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "Layout")]
    [OutputType(typeof (Item))]
    public class RemoveLayoutCommand : BaseLayoutCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[LayoutFieldId];
            if (string.IsNullOrEmpty(layoutField?.Value))
            {
                return;
            }

            var layout = LayoutDefinition.Parse(layoutField.Value);

            if (layout.Devices == null)
            {
                return;
            }

            var allDevices = layout.Devices;
            foreach (DeviceDefinition device in allDevices)
            {
                if (Device != null && !string.Equals(device.ID, Device.ID.ToString(), StringComparison.OrdinalIgnoreCase)) continue;
                layout.Devices.Remove(device);
                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
                });
                return;
            }
            if (Device != null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException(
                            $"Item \"{item.Name}\" has no layout defined for device \"{Device.Name}\""), "sitecore_layout_for_device_not_found", ErrorCategory.ObjectNotFound,
                        Device));
            }
        }
    }
}