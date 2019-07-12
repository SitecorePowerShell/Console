using System;
using System.Linq.Expressions;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsCommon.New, "SearchPredicate")]
    [OutputType(typeof(SearchResultItem))]
    public class NewSearchPredicateCommand : BaseSearchCommand
    {
        [Parameter(ParameterSetName = "Criteria", Mandatory = true)]
        public SearchCriteria[] Criteria { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        [Parameter(ParameterSetName = "Predicate")]
        public Type QueryType { get; set; }

        [Parameter(ParameterSetName = "Predicate")]
        public dynamic First { get; set; }

        [Parameter(ParameterSetName = "Predicate", Mandatory = true)]
        public dynamic Second { get; set; }

        [Parameter] public SearchOperation Operation { get; set; } = SearchOperation.And;

        protected override void EndProcessing()
        {         
            var queryableType = typeof(SearchResultItem);
            if (QueryType != null && QueryType != queryableType && QueryType.IsSubclassOf(queryableType))
            {
                queryableType = QueryType;
            }

            var objType = (dynamic)Activator.CreateInstance(queryableType);

            if (First != null && Second != null)
            {
                var shouldOr = Operation == SearchOperation.Or;
                var predicate = GetPredicateBuilder(objType, shouldOr);

                var firstPredicate = First;
                if (firstPredicate is PSObject)
                {
                    firstPredicate = (firstPredicate as PSObject)?.BaseObject;
                }

                var secondPredicate = Second;
                if (secondPredicate is PSObject)
                {
                    secondPredicate = (secondPredicate as PSObject)?.BaseObject;
                }

                if (shouldOr)
                {
                    var joinedPredicate = GetPredicateAndOr(firstPredicate, secondPredicate, true);
                    predicate = GetPredicateAndOr(predicate, joinedPredicate, true);
                    WriteObject(predicate, true);
                }
                else
                {
                    var joinedPredicate = GetPredicateAndOr(firstPredicate, secondPredicate, false);
                    predicate = GetPredicateAndOr(predicate, joinedPredicate, false);
                    WriteObject(predicate, true);
                }
            }

            if (Criteria != null)
            {
                var predicate = ProcessCriteria(objType, Criteria, Operation);
                if (predicate != null)
                {
                    WriteObject(predicate, true);
                }
            }
        }
    }
}