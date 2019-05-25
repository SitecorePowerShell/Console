using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Set, "RenderingParameter", SupportsShouldProcess = true)]
    [OutputType(typeof(RenderingDefinition))]
    public class SetRenderingParameterCommand : BaseRenderingParameterCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [Alias("Rendering")]
        public RenderingDefinition Instance { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public IDictionary Parameter { get; set; }

        [Parameter]
        public SwitchParameter Overwrite { get; set; }

        protected override void ProcessRecord()
        {
            if (!ShouldProcess(Instance.ItemID, "Set parameters for rendering"))
            {
                return;
            }

            var parameters = Overwrite.IsPresent ? new NameValueCollection() : GetParameters(Instance);

            foreach (string key in Parameter.Keys)
            {
                var value = System.Convert.ToString(Parameter[key]);
                if (parameters[key] == null)
                {
                    WriteVerbose($"Adding new key {key} to rendering parameters.");
                    parameters.Add(key, value);
                }
                else
                {
                    WriteVerbose($"Updating key {key} with a new value to the rendering parameters.");
                    parameters[key] = value;
                }
            }

            Instance.Parameters = new UrlString(parameters).ToString();
            WriteObject(Instance);
        }
    }
}