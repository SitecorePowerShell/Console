using System;
using System.Linq.Expressions;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Spe.Commandlets.Data.Search;

namespace Spe.Core.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<SearchResultItem, bool>> AddPredicate(this Expression<Func<SearchResultItem, bool>> first, Expression<Func<SearchResultItem, bool>> second, SearchOperation operation = SearchOperation.And)
        {
            switch (operation)
            {
                    case SearchOperation.And:
                        return first.And(second);
                    case SearchOperation.Or:
                        return first.Or(second);
                    default:
                        return first;
            }
        }
    }
}