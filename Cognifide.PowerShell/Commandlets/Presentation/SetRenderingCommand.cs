using System.Collections;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Rendering", SupportsShouldProcess = true)]
    public class SetRenderingCommand : BaseLayoutCommand
    {
        [Parameter(Mandatory = true)]
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
            RenderingDefinition rendering;

            var availableDevices = Device != null ? new [] { Device } : layout.Devices.ToArray();

            foreach (DeviceDefinition availableDevice in availableDevices)
            {
                foreach (var aRendering in availableDevice.Renderings.Cast<RenderingDefinition>()
                        .Where(aRendering => aRendering.UniqueId == Instance.UniqueId))
                {
                    selectedDevice = availableDevice;
                    rendering = aRendering;
                    goto RenderingFound;
                }
            }

            return;

            RenderingFound: //goto label
            rendering.ItemID = Instance.ItemID;
            rendering.Placeholder = MyInvocation.BoundParameters.ContainsKey("PlaceHolder")
                ? PlaceHolder
                : Instance.Placeholder ?? rendering.Placeholder;
            rendering.Datasource =
                !string.IsNullOrEmpty(DataSource)
                    ? DataSource
                    : Instance.Datasource;
            rendering.Cachable = Instance.Cachable;
            rendering.VaryByData = Instance.VaryByData;
            rendering.VaryByDevice = Instance.VaryByDevice;
            rendering.VaryByLogin = Instance.VaryByLogin;
            rendering.VaryByParameters = Instance.VaryByParameters;
            rendering.VaryByQueryString = Instance.VaryByQueryString;
            rendering.VaryByUser = Instance.VaryByUser;
            rendering.Parameters = Instance.Parameters;
            rendering.MultiVariateTest = Instance.MultiVariateTest;
            rendering.Rules = Instance.Rules;
            rendering.Conditions = Instance.Conditions;

            if (Parameter != null)
            {
                var parameters = new UrlString(rendering.Parameters ?? string.Empty);
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

                rendering.Parameters = parameters.ToString();
            }

            if (Index > -1)
            {
                selectedDevice.Renderings.Remove(rendering);
                selectedDevice.Insert(Index, rendering);
            }

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                LayoutField.SetFieldValue(item.Fields[LayoutFieldId], outputXml);
            });
        }
    }
}