using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using System.Data;
using Sitecore.Data.Fields;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "PlaceholderSetting", SupportsShouldProcess = true)]
    [OutputType(typeof (void))]
    public class RemovePlaceholderSettingCommand : BasePlaceholderSettingCommand
    {
        protected override void ProcessPlaceholderSettings(Item item, LayoutDefinition layout, DeviceDefinition device,
            List<PlaceholderDefinition> placeholders)
        {
            if (placeholders.Any())
            {
                if (!ShouldProcess(item.GetProviderPath(),
                    $"Remove placeholder setting(s) '{placeholders.Select(r => r.MetaDataItemId.ToString()).Aggregate((seed, curr) => seed + ", " + curr)}' from device {Device.Name}")
                )
                    return;

                foreach (
                    var placeholderSetting in
                    placeholders.Select(rendering => device.Placeholders.Cast<PlaceholderDefinition>()
                            .FirstOrDefault(r => r.UniqueId == rendering.UniqueId))
                        .Where(placeholder => placeholder != null)
                        .Reverse())
                {
                    device.Placeholders.Remove(placeholderSetting);
                }

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    LayoutField.SetFieldValue(Item.Fields[LayoutFieldId], outputXml);
                });
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find a placeholder setting to remove",
                    ErrorIds.PlaceholderSettingNotFound, ErrorCategory.ObjectNotFound, null);
            }
        }
    }
}