using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.ResolveRenderingDatasource;

namespace Cognifide.PowerShell.SitecoreIntegrations.Processors
{
    public class ScriptedRenderingDataSourceResolve : BaseScriptedDataSource
    {
        public void Process(ResolveRenderingDatasourceArgs args)
        {
            Assert.IsNotNull(args, "args");
            string source = args.Datasource;
            if (IsScripted(source))
            {
                IEnumerable<Item> items = RunEnumeration(source, Context.Item);
                args.Datasource = items.First().Paths.Path;
            }
        }
    }
}