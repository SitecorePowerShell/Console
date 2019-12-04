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
    [Cmdlet(VerbsCommon.Switch, "Rendering", SupportsShouldProcess = true)]
    [OutputType(typeof (void))]
    public class SwitchRenderingCommand : BaseRenderingCommand
    {
        [Parameter(Mandatory = true)]
        public RenderingDefinition NewRendering { get; set; }

        protected override void ProcessRenderings(Item item, LayoutDefinition layout, DeviceDefinition device,
            List<RenderingDefinition> renderings)
        {
            if (renderings.Any())
            {
                if (!ShouldProcess(item.GetProviderPath(),
                    $"Switch rendering(s) '{renderings.Select(r => r.ItemID.ToString()).Aggregate((seed, curr) => seed + ", " + curr)}' for device {Device.Name}"))
                    return;

                foreach (
                    var instanceRendering in
                        renderings.Select(rendering => device.Renderings.Cast<RenderingDefinition>()
                            .FirstOrDefault(r => r != null && r.UniqueId == rendering.UniqueId))
                            .Where(instanceRendering => instanceRendering != null)
                            .Reverse())
                {
                    DoReplaceRendering(instanceRendering, NewRendering, device);
                }

                item.Edit(p =>
                {
                    var outputXml = layout.ToXml();
                    LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
                });
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find a rendering to remove", 
                    ErrorIds.RenderingNotFound, ErrorCategory.ObjectNotFound, null);
            }
        }

        protected virtual RenderingDefinition DoReplaceRendering(
            RenderingDefinition sourceRendering,
            RenderingDefinition targetRendering, DeviceDefinition device)
        {
            var renderingDefinition = new RenderingDefinition
            {
                Cachable = sourceRendering.Cachable,
                Conditions = sourceRendering.Conditions,
                Datasource = sourceRendering.Datasource,
                ItemID = targetRendering.ItemID,
                MultiVariateTest = sourceRendering.MultiVariateTest,
                Parameters = sourceRendering.Parameters,
                Placeholder = sourceRendering.Placeholder,
                Rules = sourceRendering.Rules,
                VaryByData = sourceRendering.VaryByData,
                ClearOnIndexUpdate = sourceRendering.ClearOnIndexUpdate,
                VaryByDevice = sourceRendering.VaryByDevice,
                VaryByLogin = sourceRendering.VaryByLogin,
                VaryByParameters = sourceRendering.VaryByParameters,
                VaryByQueryString = sourceRendering.VaryByQueryString,
                VaryByUser = sourceRendering.VaryByUser
            };

            if (device.Renderings == null) return renderingDefinition;

            var index = device.Renderings.IndexOf(sourceRendering);
            device.Renderings.RemoveAt(index);
            device.Renderings.Insert(index, renderingDefinition);

            return renderingDefinition;
        }
    }
}