using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "Layout")]
    [OutputType(typeof (Item))]
    public class GetLayoutCommand : BaseLayoutCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[LayoutFieldId];
            if (layoutField == null || string.IsNullOrEmpty(layoutField.Value))
            {
                return;
            }

            var layout = LayoutDefinition.Parse(layoutField.Value);

            if (layout.Devices == null)
            {
                return;
            }

            foreach (DeviceDefinition device in layout.Devices)
            {
                if (Device == null || string.Equals(device.ID, Device.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    var layoutItem = item.Database.GetItem(device.Layout);
                    var psobj = ItemShellExtensions.GetPsObject(SessionState, layoutItem);
                    psobj.Properties.Add(new PSNoteProperty("DeviceID", device.ID));
                    var deviceItem = Device ?? item.Database.GetItem(device.ID);
                    psobj.Properties.Add(new PSNoteProperty("Device", deviceItem.Name));
                    WriteObject(psobj);
                    if (Device != null)
                    {
                        return;
                    }
                }
            }
            if (Device != null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException(
                            string.Format("Item \"{0}\" has no layout defined for device \"{1}\"", item.Name,
                                Device.Name)), "sitecore_layout_for_device_not_found", ErrorCategory.ObjectNotFound,
                        Device));
            }
        }
    }
}