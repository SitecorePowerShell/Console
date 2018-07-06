using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;
using System;
using System.Linq.Expressions;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Data.Search
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
            if (First != null && Second != null)
            {
                var shouldOr = Operation == SearchOperation.Or;
                var predicate = shouldOr
                    ? PredicateBuilder.False<SearchResultItem>()
                    : PredicateBuilder.True<SearchResultItem>();

                if (shouldOr)
                {
                    var joinedPredicate = First.Or(Second);
                    predicate = predicate.Or(joinedPredicate);
                    WriteObject(predicate, true);
                }
                else
                {
                    var joinedPredicate = First.And(Second);
                    predicate = predicate.And(joinedPredicate);
                    WriteObject(predicate, true);
                }
            }

            if (Criteria != null)
            {
                var predicate = ProcessCriteria(Criteria, Operation);
                if (predicate != null)
                {
                    WriteObject(predicate, true);
                }
            }
        }
    }
}