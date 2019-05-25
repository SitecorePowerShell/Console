using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Get, "PlaceholderSetting")]
    [OutputType(typeof (PlaceholderDefinition))]
    public class GetPlaceholderSettingCommand : BasePlaceholderSettingCommand
    {
        protected override void ProcessPlaceholderSettings(Item item, LayoutDefinition layout, DeviceDefinition device, List<PlaceholderDefinition> placeholders)
        {
            placeholders.ForEach(p => WriteObject(ItemShellExtensions.WrapInItemOwner(SessionState, item, p)));
        }
    }
}