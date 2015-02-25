using System.Collections;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "Rendering",SupportsShouldProcess = true)]
    public class SetRenderingCommand : BaseLanguageAgnosticItemCommand
    {
        private int index = -1;

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
        public int Index
        {
            get { return index; }
            set { index = value; }
        }

        protected override void ProcessItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(),
                string.Format("Set '{0}' rendering parameters. Rendering is of type: {1}", Instance.UniqueId,
                    Instance.ItemID)))
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
                rendering.Placeholder = MyInvocation.BoundParameters.ContainsKey("PlaceHolder") ? PlaceHolder : Instance.Placeholder ?? rendering.Placeholder;
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
                        if (parameters.Parameters.AllKeys.Contains(name))
                        {
                            parameters.Parameters[name] = Parameter[name].ToString();
                        }
                        else
                        {
                            parameters.Add(name, Parameter[name].ToString());
                        }
                    rendering.Parameters = parameters.ToString();
                }

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
}