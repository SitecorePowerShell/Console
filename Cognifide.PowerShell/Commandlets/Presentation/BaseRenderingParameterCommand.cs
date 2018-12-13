using System.Collections.Specialized;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Layouts;
using Sitecore.Web;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    public class BaseRenderingParameterCommand : BaseShellCommand
    {
        protected NameValueCollection GetParameters(RenderingDefinition renderingDefinition)
        {
            return WebUtil.ParseUrlParameters(renderingDefinition.Parameters ?? string.Empty);
        }
    }
}