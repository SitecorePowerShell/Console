using System.Collections.Generic;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "PlaceholderSetting")]
    [OutputType(typeof (RenderingDefinition))]
    public class GetPlaceholderSettingCommand : BasePlaceholderSettingCommand
    {
        protected override void ProcessPlaceholderSettings(Item item, LayoutDefinition layout, DeviceDefinition device, List<PlaceholderDefinition> placeholders)
        {
            placeholders.ForEach(p => WriteObject(ItemShellExtensions.WrapInItemOwner(SessionState, item, p)));
        }
    }
}