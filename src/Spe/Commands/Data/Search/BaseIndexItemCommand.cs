using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data.Items;
using Spe.Core.Extensions;

namespace Spe.Commands.Data.Search
{
    public abstract class BaseIndexItemCommand : BaseIndexCommand
    {
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Name")]
        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "Item")]
        public Item Item { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true, ParameterSetName = "SearchResultItem")]
        public SearchResultItem SearchResultItem { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                var itemPath = Item.Paths.Path;
                var indexable = new SitecoreIndexableItem(Item);

                foreach (var index in WildcardFilter(Name, ContentSearchManager.Indexes, index => index.Name))
                {
                    if (IndexIsValidForItem(index, Item))
                        ProcessIndexable(index, indexable, itemPath);
                }
            }
            else if (SearchResultItem != null)
            {
                var itemPath = SearchResultItem.Path;
                var indexable = new SitecoreIndexableItem(SearchResultItem.GetItem());
                var indexname = SearchResultItem.Fields["_indexname"].ToString();

                foreach (
                    var index in WildcardFilter(indexname, ContentSearchManager.Indexes, index => index.Name))
                {
                    ProcessIndexable(index, indexable, itemPath);
                }
            }
        }

        /// <summary>
        /// Allows commands to override whether this index is applicable for the action. Defaults to true.
        /// </summary>
        protected virtual bool IndexIsValidForItem(ISearchIndex index, Item item) => true;

        protected abstract void ProcessIndexable(ISearchIndex index, IIndexable indexable, string itemPath);
    }
}