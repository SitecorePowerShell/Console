using System.Collections;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Rendering", SupportsShouldProcess = true)]
    public class SetRenderingCommand : BaseLayoutCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        [Parameter]
        public Hashtable Parameter { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; }

        [Parameter]
        public string DataSource { get; set; }

        [Parameter]
        public int Index { get; set; } = -1;

        [Parameter]
        public virtual DeviceItem Device { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (!ShouldProcess(item.GetProviderPath(),
                $"Set '{Instance.UniqueId}' rendering parameters. Rendering is of type: {Instance.ItemID}")) return;

            LayoutField layoutField = item.Fields[LayoutFieldId];
            if (layoutField == null || string.IsNullOrEmpty(layoutField.Value))
            {
                return;
            }

            var layout = LayoutDefinition.Parse(layoutField.Value);

            DeviceDefinition selectedDevice;
            RenderingDefinition selectedRendering;

            var availableDevices = layout.Devices.Cast<DeviceDefinition>();
            if (Device != null)
            {
                availableDevices = availableDevices.Where(d => d.ID == Device.ID.ToString());
            }
            
            foreach (var availableDevice in availableDevices)
            {
                foreach (var availableRendering in availableDevice.Renderings.Cast<RenderingDefinition>()
                        .Where(aRendering => aRendering.UniqueId == Instance.UniqueId))
                {
                    selectedDevice = availableDevice;
                    selectedRendering = availableRendering;
                    goto RenderingFound;
                }
            }

            return;

            RenderingFound: //goto label
            selectedRendering.ItemID = Instance.ItemID;
            selectedRendering.Placeholder = MyInvocation.BoundParameters.ContainsKey("PlaceHolder")
                ? PlaceHolder
                : Instance.Placeholder ?? selectedRendering.Placeholder;
            selectedRendering.Datasource =
                !string.IsNullOrEmpty(DataSource)
                    ? DataSource
                    : Instance.Datasource;
            selectedRendering.Cachable = Instance.Cachable;
            selectedRendering.VaryByData = Instance.VaryByData;
            selectedRendering.VaryByDevice = Instance.VaryByDevice;
            selectedRendering.VaryByLogin = Instance.VaryByLogin;
            selectedRendering.VaryByParameters = Instance.VaryByParameters;
            selectedRendering.VaryByQueryString = Instance.VaryByQueryString;
            selectedRendering.VaryByUser = Instance.VaryByUser;
            selectedRendering.Parameters = Instance.Parameters;
            selectedRendering.MultiVariateTest = Instance.MultiVariateTest;
            selectedRendering.Rules = Instance.Rules;
            selectedRendering.Conditions = Instance.Conditions;

            if (Parameter != null)
            {
                var parameters = new UrlString(selectedRendering.Parameters ?? string.Empty);
                foreach (string name in Parameter.Keys)
                {
                    if (parameters.Parameters.AllKeys.Contains(name))
                    {
                        parameters.Parameters[name] = Parameter[name].ToString();
                    }
                    else
                    {
                        parameters.Add(name, Parameter[name].ToString());
                    }
                }

                selectedRendering.Parameters = parameters.ToString();
            }

            if (Index > -1)
            {
                selectedDevice.Renderings.Remove(selectedRendering);
                selectedDevice.Insert(Index, selectedRendering);
            }

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
            });
        }
    }
}