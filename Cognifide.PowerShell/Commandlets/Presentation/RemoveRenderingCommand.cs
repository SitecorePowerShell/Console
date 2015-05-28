using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "Rendering", SupportsShouldProcess = true)]
    [OutputType(typeof (void))]
    public class RemoveRenderingCommand : BaseRenderingCommand
    {
        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            IEnumerable<RenderingDefinition> renderings)
        {
            if (ShouldProcess(item.GetProviderPath(),
                string.Format("Remove rendering(s) '{0}' from device {1}",
                    renderings.Select(r => r.ItemID.ToString()).Aggregate((seed, curr) => seed + ", " + curr),
                    Device.Name)))
            {
                foreach (
                    var instanceRendering in
                        renderings.Select(rendering => device.Renderings.Cast<RenderingDefinition>()
                            .FirstOrDefault(r => r.UniqueId == rendering.UniqueId))
                            .Where(instanceRendering => instanceRendering != null))
                {
                    device.Renderings.Remove(instanceRendering);
                }

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    Item["__Renderings"] = outputXml;
                });
            }
        }
    }
}