using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "RenderingParameters")]
    [OutputType(typeof(RenderingDefinition))]
    public class RemoveRenderingParametersCommand : BaseRenderingParametersCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public RenderingDefinition Rendering { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string[] Parameter { get; set; }

        protected override void ProcessRecord()
        {
            var parameters = this.GetParameters(Rendering);

            foreach (var name in Parameter)
            {
                parameters.Remove(name);
            }

            Rendering.Parameters = new UrlString(parameters).ToString();
            WriteObject(Rendering);
        }
    }
}