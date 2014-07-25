using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "Rendering")]
    [OutputType(new[] {typeof (RenderingDefinition)})]
    public class GetRenderingCommand : BaseRenderingCommand
    {

        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            IEnumerable<RenderingDefinition> renderings)
        {
            renderings.ToList().ForEach(r => WriteObject(ItemShellExtensions.WrapInItemOwner(SessionState,item,r)));

        }
    }
}