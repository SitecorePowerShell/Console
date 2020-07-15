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

        [Parameter(ParameterSetName = "Dynamic")]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public object[] FilterValues { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        [Parameter(ParameterSetName = "Dynamic")]
        [Parameter(ParameterSetName = "Predicate")]
        [Parameter(ParameterSetName = "ScopeQuery")]
        public string[] FacetOn { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        [Parameter(ParameterSetName = "Dynamic")]
        [Parameter(ParameterSetName = "Predicate")]
        [Parameter(ParameterSetName = "ScopeQuery")]
        public int FacetMinCount { get; set; }

        [Parameter]
        public Type QueryType { get; set; }

        [Alias("Predicate")]
        [Parameter(ParameterSetName = "Predicate")]
        public dynamic WherePredicate { get; set; }

        [Parameter(ParameterSetName = "Predicate")]
        public dynamic FilterPredicate { get; set; }

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

        [Parameter]
        public string[] Property { get; set; }

        protected override void EndProcessing()
        {
            var index = String.IsNullOrEmpty(Index) ? "sitecore_master_index" : Index;

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
                    query = ApplyWhereAndValues(query, Where, WhereValues);
                }
                else
                {
                    if (WhereValues != null)
                    {
                        WriteWarning($"The parameter ({nameof(Where)}) containing the dynamic Linq is missing.");
                    }
                }

                if (!string.IsNullOrEmpty(Filter))
                {
                    query = ApplyFilterAndValues(query, Filter, FilterValues);
                }
                else
                {
                    if (FilterValues != null)
                    {
                        WriteWarning($"The parameter ({nameof(Filter)}) containing the dynamic Linq is missing.");
                    }
                }

                if (Criteria != null)
                {
                    var criteriaPredicate = ProcessCriteria(objType, Criteria, SearchOperation.And);
                    query = ApplyWhere(query, criteriaPredicate);
                }

                if (WherePredicate != null)
                {
                    var boxedPredicate = WherePredicate;
                    if (boxedPredicate is PSObject)
                    {
                        boxedPredicate = (WherePredicate as PSObject)?.BaseObject;
                    }
                    query = ApplyWhere(query, boxedPredicate);
                }

                if (FilterPredicate != null)
                {
                    var boxedPredicate = FilterPredicate;
                    if (boxedPredicate is PSObject)
                    {
                        boxedPredicate = (FilterPredicate as PSObject)?.BaseObject;
                    }
                    query = ApplyFilter(query, boxedPredicate);
                }

                if (ScopeQuery != null)
                {
                    query = ProcessScopeQuery(query, context, ScopeQuery);
                }

                if (FacetOn != null)
                {
                    var facets = ApplyFacetOn(query, FacetOn, FacetMinCount);
                    WriteObject(facets, true);
                    return;
                }

                if (!string.IsNullOrEmpty(OrderBy))
                {
                    query = ApplyOrderBy(query, OrderBy);
                }

                if (Property != null)
                {
                    // The use of Last is not supported because it requires Concat. Concat is not supported by Sitecore.
                    query = ApplySkipAndTake(query, First, Skip);
                    if (Last > 0)
                    {
                        WriteWarning($"The use of {nameof(Last)} is not supported when selecting with {nameof(Property)}.");
                    }

                    var queryableSelectProperties = ApplySelect(query, Property);
                    WriteObject(queryableSelectProperties, true);
                }
                else
                {
                    WriteObject(ApplySkipAndTakeByPosition(query, First, Last, Skip), true);
                }
            }
        }
    }
}