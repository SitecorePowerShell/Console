using System.Management.Automation;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Remove, "RenderingParameter", SupportsShouldProcess = true)]
    [OutputType(typeof(RenderingDefinition))]
    public class RemoveRenderingParameterCommand : BaseRenderingParameterCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string[] Parameter { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Instance.ItemID, "Remove parameters from rendering"))
            {
                return;
            }

            var parameters = this.GetParameters(Instance);

            foreach (var key in Parameter)
            {
                WriteVerbose($"Removing the rendering parameter with key {key}.");
                parameters.Remove(key);
            }

            Instance.Parameters = new UrlString(parameters).ToString();
            WriteObject(Instance);
        }
    }
}