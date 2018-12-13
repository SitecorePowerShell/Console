using System.Management.Automation;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "RenderingParameter")]
    [OutputType(typeof(RenderingDefinition))]
    public class RemoveRenderingParameterCommand : BaseRenderingParameterCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public RenderingDefinition Rendering { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string[] Parameter { get; set; }

        protected override void ProcessRecord()
        {
            var parameters = this.GetParameters(Rendering);

            foreach (var key in Parameter)
            {
                WriteVerbose($"Removing the rendering parameter with key {key}.");
                parameters.Remove(key);
            }

            Rendering.Parameters = new UrlString(parameters).ToString();
            WriteObject(Rendering);
        }
    }
}