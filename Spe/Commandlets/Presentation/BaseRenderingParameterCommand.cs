using System.Collections.Specialized;
using Sitecore.Layouts;
using Sitecore.Web;
using Spe.Commandlets.Interactive;

namespace Spe.Commandlets.Presentation
{
    public class BaseRenderingParameterCommand : BaseShellCommand
    {
        protected NameValueCollection GetParameters(RenderingDefinition renderingDefinition)
        {
            return WebUtil.ParseUrlParameters(renderingDefinition.Parameters ?? string.Empty);
        }
    }
}