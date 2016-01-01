using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using System.Data;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "Rendering", SupportsShouldProcess = true)]
    [OutputType(typeof (void))]
    public class RemoveRenderingCommand : BaseRenderingCommand
    {
        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            IEnumerable<RenderingDefinition> renderings)
        {
            if (renderings.Any())
            {
                if (!ShouldProcess(item.GetProviderPath(),
                    $"Remove rendering(s) '{renderings.Select(r => r.ItemID.ToString()).Aggregate((seed, curr) => seed + ", " + curr)}' from device {Device.Name}"))
                    return;

                foreach (
                    var instanceRendering in
                        renderings.Select(rendering => device.Renderings.Cast<RenderingDefinition>()
                            .FirstOrDefault(r => r.UniqueId == rendering.UniqueId))
                            .Where(instanceRendering => instanceRendering != null)
                            .Reverse())
                {
                    device.Renderings.Remove(instanceRendering);
                }

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    Item[LayoutFieldId] = outputXml;
                });
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find a rendering to remove", 
                    ErrorIds.RenderingNotFound, ErrorCategory.ObjectNotFound, null);
            }
        }
    }
}