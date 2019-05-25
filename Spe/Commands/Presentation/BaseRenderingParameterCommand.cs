using System.Collections.Specialized;
using Sitecore.Layouts;
using Sitecore.Web;
using Spe.Commands.Interactive;

namespace Spe.Commands.Presentation
{
    public class BaseRenderingParameterCommand : BaseShellCommand
    {
        protected NameValueCollection GetParameters(RenderingDefinition renderingDefinition)
        {
            return WebUtil.ParseUrlParameters(renderingDefinition.Parameters ?? string.Empty);
        }
    }
}