using System.Linq;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.ResolveRenderingDatasource;

namespace Cognifide.PowerShell.Integrations.Processors
{
    public class ScriptedRenderingDataSourceResolve : BaseScriptedDataSource
    {
        public void Process(ResolveRenderingDatasourceArgs args)
        {
            Assert.IsNotNull(args, "args");
            var source = args.Datasource;
            if (IsScripted(source))
            {
                var items = RunEnumeration(source, Context.Item);
                args.Datasource = items.First().Paths.Path;
            }
        }
    }
}