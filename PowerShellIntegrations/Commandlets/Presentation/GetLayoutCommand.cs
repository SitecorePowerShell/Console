using System;
using System.Data;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "Layout")]
    [OutputType(new[] {typeof (Item)})]
    public class GetLayoutCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[FieldIDs.LayoutField];
            if (layoutField == null || string.IsNullOrEmpty(layoutField.Value))
            {
                return;
            }

            LayoutDefinition layout = LayoutDefinition.Parse(layoutField.Value);

            if (layout.Devices == null)
            {
                return;
            }

            foreach (DeviceDefinition device in layout.Devices)
            {
                if (Device == null || string.Equals(device.ID, Device.ID.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    Item layoutItem = item.Database.GetItem(device.Layout);
                    WriteItem(layoutItem);
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