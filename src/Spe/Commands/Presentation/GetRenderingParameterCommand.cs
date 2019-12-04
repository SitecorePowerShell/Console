using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Management.Automation;
using Sitecore.Layouts;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Get, "RenderingParameter")]
    [OutputType(typeof(IDictionary))]
    public class GetRenderingParameterCommand : BaseRenderingParameterCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        [Parameter]
        [Alias("Key")]
        public string[] Name { get; set; }

        protected override void ProcessRecord()
        {
            var parameters = GetParameters(Instance);
            var allKeys = parameters.AllKeys;
            var keys = Name ?? parameters.AllKeys;
            var mapping = new OrderedDictionary();
            foreach (var key in keys)
            {
                if(!allKeys.Contains(key)) continue;
                var value = parameters.Get(key);
                mapping.Add(key, value);
            }

            if (mapping.Count > 0)
            {
                WriteObject(mapping);
            }
        }
    }
}