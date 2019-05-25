using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "Rendering", SupportsShouldProcess = true)]
    [OutputType(typeof (void))]
    public class RemoveRenderingCommand : BaseRenderingCommand
    {
        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            List<RenderingDefinition> renderings)
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
                    LayoutField.SetFieldValue(Item.Fields[LayoutFieldId], outputXml);
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