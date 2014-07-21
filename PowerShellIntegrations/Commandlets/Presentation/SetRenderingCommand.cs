using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Rendering")]
    public class SetRenderingCommand : BaseItemCommand
    {
        private int index = -1;

        [Parameter(Mandatory = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        // override to hide as rengedings are not language sensitive
        public override string[] Language { get; set; }

        [Parameter]
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        protected override void ProcessItem(Item item)
        {
            LayoutField layoutField = item.Fields[FieldIDs.LayoutField];
            if (layoutField == null || string.IsNullOrEmpty(layoutField.Value))
            {
                return;
            }

            LayoutDefinition layout = LayoutDefinition.Parse(layoutField.Value);

            DeviceDefinition device;
            RenderingDefinition rendering;

            foreach (DeviceDefinition aDevice in layout.Devices)
            {
                foreach (RenderingDefinition aRendering in aDevice.Renderings)
                {
                    if (aRendering.UniqueId != Instance.UniqueId) continue;

                    device = aDevice;
                    rendering = aRendering;
                    goto Renderingfound;
                    // Yes I used goto, cry me a river!
                    // http://xkcd.com/292/
                }
            }

            return;

            Renderingfound: //goto label
            rendering.ItemID = Instance.ItemID;
            rendering.Placeholder = Instance.Placeholder;
            rendering.Datasource = Instance.Datasource;
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

            if (Index > -1)
            {
                device.Renderings.Remove(rendering);
                device.Insert(index, rendering);
            }

            item.Edit(p =>
            {
                var outputXml = layout.ToXml();
                Item["__Renderings"] = outputXml;
            });
        }
    }
}