using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.New, "Rendering")]
    [OutputType(typeof (RenderingDefinition),
        ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class NewRenderingCommand : BaseItemCommand
    {
        [Parameter]
        public Hashtable Parameter { get; set; }

        [Parameter]
        public string PlaceHolder { get; set; }

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
            var rendering = new RenderingDefinition
            {
                ItemID = item.ID.ToString(),
                Placeholder = PlaceHolder,
                Datasource = DataSource != null ? DataSource.ID.ToString() : null,
                Cachable = Cacheable ? "1" : null,
                VaryByData = VaryByData ? "1" : null,
                VaryByDevice = VaryByDevice ? "1" : null,
                VaryByLogin = VaryByLogin ? "1" : null,
                VaryByParameters = VaryByParameters ? "1" : null,
                VaryByQueryString = VaryByQueryString ? "1" : null,
                VaryByUser = VaryByUser ? "1" : null
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
            var psobj = ItemShellExtensions.WrapInItemOwner(SessionState, item, rendering);
            WriteObject(psobj);
        }
    }
}