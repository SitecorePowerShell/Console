using System;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Spe.Core.Validation;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsCommon.Find, "Item", DefaultParameterSetName = "Criteria")]
    [OutputType(typeof(SearchResultItem))]
    public class FindItemCommand : BaseSearchCommand
    {
        public static string[] Indexes
        {
            get { return ContentSearchManager.Indexes.Select(i => i.Name).ToArray(); }
        }

        [AutocompleteSet(nameof(Indexes))]
        [Parameter(Mandatory = true, Position = 0)]
        public string Index { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        public SearchCriteria[] Criteria { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public string Where { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public object[] WhereValues { get; set; }

        [Parameter]
        public Type QueryType { get; set; }

        [Parameter(ParameterSetName = "Predicate")]
        public dynamic Predicate { get; set; }

        [Parameter(ParameterSetName = "ScopeQuery")]
        public string ScopeQuery { get; set; }

        [Parameter]
        public string OrderBy { get; set; }

        [Parameter]
        public int First { get; set; }

        [Parameter]
        public int Last { get; set; }

        [Parameter]
        public int Skip { get; set; }

        protected override void EndProcessing()
        {
            var index = string.IsNullOrEmpty(Index) ? "sitecore_master_index" : Index;

            using (var context = ContentSearchManager.GetIndex(index).CreateSearchContext())
            {
                var queryableType = typeof(SearchResultItem);
                if (QueryType != null && QueryType != queryableType && QueryType.IsSubclassOf(queryableType))
                {
                    queryableType = QueryType;
                }

                var objType = (dynamic)Activator.CreateInstance(queryableType);
                var query = GetQueryable(objType, context);

                if (!string.IsNullOrEmpty(Where))
                {
                    query = WhereAndValues(query, Where, WhereValues);
                }

                if (Criteria != null)
                {
                    var criteriaPredicate = ProcessCriteria(objType, Criteria, SearchOperation.And);
                    query = WherePredicate(query, criteriaPredicate);
                }

                if (Predicate != null)
                {
                    var boxedPredicate = Predicate;
                    if (boxedPredicate is PSObject)
                    {
                        boxedPredicate = (Predicate as PSObject)?.BaseObject;
                    }
                    query = WherePredicate(query, boxedPredicate);
                }

                if (ScopeQuery != null)
                {
                    query = ProcessScopeQuery(query, context, ScopeQuery);
                }

                if (!string.IsNullOrEmpty(OrderBy))
                {
                    query = OrderIfSupported(query, OrderBy);
                }

                WriteObject(FilterByPosition(query, First, Last, Skip), true);
            }
        }
    }
}