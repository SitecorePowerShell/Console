using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetRenderingDatasource;

namespace Cognifide.PowerShell.Integrations.Processors
{
    public class ScriptedRenderingDataSourceRoots : BaseScriptedDataSource
    {
        public void Process(GetRenderingDatasourceArgs args)
        {
            Assert.IsNotNull(args, "args");
            string sources = args.RenderingItem["Datasource Location"];
            if (IsScripted(sources))
            {
                var items = new ItemList();
                Item contextItem = args.ContentDatabase.GetItem(args.ContextItemPath);
                GetScriptedQueries(sources, contextItem, items);

                args.DatasourceRoots.AddRange(items);
            }
        }
    }
}