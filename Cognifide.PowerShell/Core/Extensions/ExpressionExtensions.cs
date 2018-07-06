using System;
using System.Linq.Expressions;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class ExpressionExtensions
    {
        public static Expression<Func<SearchResultItem, bool>> AddPredicate(this Expression<Func<SearchResultItem, bool>> first, Expression<Func<SearchResultItem, bool>> second, bool shouldOr = false)
        {
            return shouldOr ? first.Or(second) : first.And(second);
        }
    }
}