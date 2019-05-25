using System.Linq;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.ResolveRenderingDatasource;

namespace Spe.Integrations.Processors
{
    public class ScriptedRenderingDataSourceResolve : BaseScriptedDataSource
    {
        public void Process(ResolveRenderingDatasourceArgs args)
        {
            Assert.IsNotNull(args, "args");
            var source = args.Datasource;
            if (!IsScripted(source)) return;

            var items = RunEnumeration(source, Context.Item);
            args.Datasource = items.Select(item => item.ID.ToString()).Aggregate((current, next) => current + "|" + next);
        }
    }
}