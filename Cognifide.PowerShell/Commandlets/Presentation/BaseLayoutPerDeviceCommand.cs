using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    public abstract class BaseLayoutPerDeviceCommand : BaseLayoutCommand
    {
        [Parameter]
        public virtual DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[LayoutFieldId];
            if (layoutField != null && !string.IsNullOrEmpty(layoutField.Value))
            {
                var layout = LayoutDefinition.Parse(layoutField.Value);
                if (Device == null)
                {
                    Device = CurrentDatabase.Resources.Devices.GetAll().FirstOrDefault(d => d.IsDefault);
                }

                if (Device == null)
                {
                    WriteError(
                        new ErrorRecord(
                            new ObjectNotFoundException(
                                "Device not provided and no default device in the system is defined."),
                            "sitecore_device_not_found", ErrorCategory.InvalidData, null));
                    return;
                }

                var device = layout.GetDevice(Device.ID.ToString());
                if (device != null)
                {
                    ProcessLayout(item, layout, device);
                }
            }
        }

        protected abstract void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device);
    }
}