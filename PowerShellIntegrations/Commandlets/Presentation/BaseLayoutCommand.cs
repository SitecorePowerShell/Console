using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    public abstract class BaseLayoutCommand : BaseItemCommand
    {
        [Parameter]
        public virtual DeviceItem Device { get; set; }

        // override to hide as rengedings are not language sensitive
        public override string[] Language { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[FieldIDs.LayoutField];
            if (layoutField != null && !string.IsNullOrEmpty(layoutField.Value))
            {
                LayoutDefinition layout = LayoutDefinition.Parse(layoutField.Value);
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

                DeviceDefinition device = layout.GetDevice(Device.ID.ToString());
                if (device != null)
                {
                    ProcessLayout(item, layout, device);
                }
            }
        }

        protected abstract void ProcessLayout(Item item, LayoutDefinition layout, DeviceDefinition device);
    }
}