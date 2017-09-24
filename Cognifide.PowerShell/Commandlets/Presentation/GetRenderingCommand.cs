using System.Collections.Generic;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "Rendering")]
    [OutputType(typeof (RenderingDefinition))]
    public class GetRenderingCommand : BaseRenderingCommand
    {
        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            List<RenderingDefinition> renderings)
        {
            renderings.ForEach(r => WriteObject(ItemShellExtensions.WrapInItemOwner(SessionState, item, r)));
        }
    }
}