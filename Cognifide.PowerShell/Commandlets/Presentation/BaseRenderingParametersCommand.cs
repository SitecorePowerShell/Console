using System.Collections.Specialized;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Diagnostics;
using Sitecore.Layouts;
using Sitecore.Text;
using Sitecore.Web;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    public class BaseRenderingParametersCommand : BaseShellCommand
    {
        protected NameValueCollection GetParameters(RenderingDefinition renderingDefinition)
        {
            return WebUtil.ParseUrlParameters(renderingDefinition.Parameters ?? string.Empty);
        }
    }
}