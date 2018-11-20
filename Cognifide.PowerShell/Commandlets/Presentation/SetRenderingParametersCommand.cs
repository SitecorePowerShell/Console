using System.Collections;
using System.Collections.Specialized;
using System.Management.Automation;
using Sitecore.Layouts;
using Sitecore.Text;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsCommon.Set, "RenderingParameters")]
    [OutputType(typeof(RenderingDefinition))]
    public class SetRenderingParametersCommand : BaseRenderingParametersCommand
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
                parameters.Add(key, System.Convert.ToString(Parameter[key]));
            }

            Rendering.Parameters = new UrlString(parameters).ToString();
            WriteObject(Rendering);
        }
    }
}