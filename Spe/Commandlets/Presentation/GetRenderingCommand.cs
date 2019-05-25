using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;

namespace Spe.Commandlets.Presentation
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