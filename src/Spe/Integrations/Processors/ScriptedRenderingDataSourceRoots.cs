using Sitecore.Collections;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetRenderingDatasource;

namespace Spe.Integrations.Processors
{
    public class ScriptedRenderingDataSourceRoots : BaseScriptedDataSource
    {
        public void Process(GetRenderingDatasourceArgs args)
        {
            Assert.IsNotNull(args, "args");
            var sources = args.RenderingItem["Datasource Location"];
            if (!IsScripted(sources)) return;

            var items = new ItemList();
            var contextItem = args.ContentDatabase.GetItem(args.ContextItemPath);
            GetScriptedQueries(sources, contextItem, items);

            args.DatasourceRoots.AddRange(items);
        }
    }
}