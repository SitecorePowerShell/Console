using System.Collections;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Add, "Rendering")]
    [OutputType(new[] {typeof (RenderingReference)},
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class AddRenderingCommand : BaseItemCommand
    {
        [Parameter]
        public DeviceItem Device { get; set; }

        [Parameter(Mandatory = true)]
        public Item Rendering { get; set; }

        [Parameter]
        public Hashtable Parameter { get; set; }

        [Parameter(Mandatory = true)]
        public string Placeholder { get; set; }

        [Parameter]
        public Item DataSource { get; set; }

        [Parameter]
        public SwitchParameter Cacheable { get; set; }

        [Parameter]
        public SwitchParameter VaryByData { get; set; }

        [Parameter]
        public SwitchParameter VaryByDevice { get; set; }

        [Parameter]
        public SwitchParameter VaryByLogin { get; set; }

        [Parameter]
        public SwitchParameter VaryByParameters { get; set; }

        [Parameter]
        public SwitchParameter VaryByQueryString { get; set; }

        [Parameter]
        public SwitchParameter VaryByUser { get; set; }

        protected override void ProcessItem(Item item)
        {
            LayoutDefinition layout = LayoutDefinition.Parse(Item[FieldIDs.LayoutField]);
            if (Device == null)
            {
                Device = CurrentDatabase.Resources.Devices.GetAll().FirstOrDefault(d => d.IsDefault);
            }
            if (Device == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException(
                            "Device not provided and no default device in the system is defined."),
                        "sitecore_device_not_found", ErrorCategory.InvalidData, null));
                return;
            }

            DeviceDefinition device = layout.GetDevice(Device.ID.ToString());

            var rendering = new RenderingDefinition
            {
                ItemID = Rendering.ID.ToString(),
                Placeholder = Placeholder,
                Datasource = DataSource != null ? DataSource.ID.ToString() : null,
                Cachable = Cacheable ? "1" : null,
                VaryByData = VaryByData ? "1" : null,
                VaryByDevice = VaryByDevice ? "1" : null,
                VaryByLogin = VaryByLogin ? "1" : null,
                VaryByParameters = VaryByParameters ? "1" : null,
                VaryByQueryString = VaryByQueryString ? "1" : null,
                VaryByUser = VaryByUser ? "1" : null,
            };

            if (Parameter != null)
            {
                var parameters = new UrlString();
                foreach (string name in Parameter.Keys)
                    parameters.Add(name, Parameter[name].ToString());
                rendering.Parameters = parameters.ToString();
            }

            //todo: add support for conditions
            //renderingDefinition.Conditions
            //todo: add support for multivariate tests
            //rendering.MultiVariateTest

            device.AddRendering(rendering);

            item.Edit(p =>
            {
                string outputXml = layout.ToXml();
                Item["__Renderings"] = outputXml;
            });
        }
    }
}