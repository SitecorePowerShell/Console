using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Shell.Framework.Commands.TemplateBuilder;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "Rendering", SupportsShouldProcess = true)]
    [OutputType(new[] {typeof (void)})]
    public class RemoveRenderingCommand : BaseRenderingCommand
    {

        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device, IEnumerable<RenderingDefinition> renderings)
        {
            if (ShouldProcess(item.GetProviderPath(),
                string.Format("Remove rendering(s) '{0}' from device {1}",
                    renderings.Select(r => r.ItemID.ToString()).Aggregate((seed, curr) => seed + ", " + curr),
                    Device.Name)))
            {
                foreach (var rendering in renderings)
                {
                    var instanceRendering =
                        device.Renderings.Cast<RenderingDefinition>()
                            .FirstOrDefault(r => r.UniqueId == rendering.UniqueId);
                    if (instanceRendering != null)
                    {
                        device.Renderings.Remove(instanceRendering);
                    }
                }

                item.Edit(p =>
                {
                    string outputXml = layout.ToXml();
                    Item["__Renderings"] = outputXml;
                });
            }
        }

    }
}