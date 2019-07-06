using System;
using System.Linq.Expressions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Utilities;
using Spe.Commands.Data.Search;

namespace Spe.Core.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<T, bool>> AddPredicate<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second, SearchOperation operation = SearchOperation.And) where T : ISearchResult
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