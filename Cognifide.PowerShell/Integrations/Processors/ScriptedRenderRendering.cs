using Sitecore.Diagnostics;
using Sitecore.Mvc.Pipelines.Response.RenderRendering;
using Sitecore.Pipelines;
using Sitecore.Pipelines.ResolveRenderingDatasource;
using Sitecore.StringExtensions;

namespace Cognifide.PowerShell.Integrations.Processors
{
    public class ScriptedRenderRendering : BaseScriptedDataSource
    {
        public void Process(RenderRenderingArgs args)
        {
            Assert.IsNotNull(args, "args");

            if (args.Rendering.DataSource.IsNullOrEmpty()) return;
            if (!args.Rendering.DataSource.StartsWith("script:")) return;

            var renderingDatasourceArgs = new ResolveRenderingDatasourceArgs(args.Rendering.DataSource);
            CorePipeline.Run("resolveRenderingDatasource", renderingDatasourceArgs);

            args.Rendering.DataSource = renderingDatasourceArgs.Datasource;
        }
    }
}