using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "RenderingParameter")]
    [OutputType(typeof(RenderingDefinition))]
    public class SetRenderingParameterCommand : BaseRenderingParameterCommand
    {
        [Parameter(Mandatory = true, Position = 0)]
        public RenderingDefinition Rendering { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public Hashtable Parameter { get; set; }

        [Parameter]
        public SwitchParameter Overwrite { get; set; }

        protected override void ProcessRecord()
        {
            var parameters = Overwrite.IsPresent ? new NameValueCollection() : GetParameters(Rendering);

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

            Rendering.Parameters = new UrlString(parameters).ToString();
            WriteObject(Rendering);
        }
    }
}