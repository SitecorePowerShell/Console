using System.Collections;
using System.Linq;
using System.Management.Automation;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Get, "RenderingParameters")]
    [OutputType(typeof(Hashtable))]
    public class GetRenderingParametersCommand : BaseRenderingParametersCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public RenderingDefinition Rendering { get; set; }

        protected override void ProcessRecord()
        {
            var parameters = GetParameters(Rendering);
            WriteObject(new Hashtable(parameters.AllKeys.ToDictionary(k => k, k => parameters[k])));
        }
    }
}