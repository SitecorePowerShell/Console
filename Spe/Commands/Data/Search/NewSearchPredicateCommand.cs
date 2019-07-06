using System;
using System.Linq.Expressions;
using System.Management.Automation;
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

        [Parameter(ParameterSetName = "Predicate")]
        public Expression<Func<SearchResultItem, bool>> First { get; set; }

        [Parameter(ParameterSetName = "Predicate", Mandatory = true)]
        public Expression<Func<SearchResultItem, bool>> Second { get; set; }

        [Parameter] public SearchOperation Operation { get; set; } = SearchOperation.And;

        protected override void EndProcessing()
        {         
            var queryableType = typeof(SearchResultItem);
            var objType = (dynamic)Activator.CreateInstance(queryableType);

            if (First != null && Second != null)
            {
                var shouldOr = Operation == SearchOperation.Or;
                var predicate = GetPredicateBuilder(objType, shouldOr);

                if (shouldOr)
                {
                    var joinedPredicate = First.Or(Second);
                    predicate = GetPredicateAndOr(predicate, joinedPredicate, true);
                    WriteObject(predicate, true);
                }
                else
                {
                    var joinedPredicate = First.And(Second);
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