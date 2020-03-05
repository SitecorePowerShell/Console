using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Spe.Core.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;

namespace Spe.Commands.Data.Search
{
    public class BaseSearchCommand : BaseCommand
    {
        public Expression<Func<T, bool>> ProcessCriteria<T>(T queryableType, SearchCriteria[] criterias, SearchOperation operation) where T : ISearchResult
        {
            var predicate = operation == SearchOperation.Or
                ? PredicateBuilder.False<T>()
                : PredicateBuilder.True<T>();

            if (criterias == null) return predicate;

            foreach (var criteria in criterias)
            {
                if (criteria.Value == null) continue;
                var boost = criteria.Boost;
                var comparer = criteria.CaseSensitive.HasValue && criteria.CaseSensitive.Value
                    ? StringComparison.Ordinal
                    : StringComparison.OrdinalIgnoreCase;
                switch (criteria.Filter)
                {
                    case FilterType.DescendantOf:
                        var root = ObjectToString(criteria.Value);

                        if (string.IsNullOrEmpty(root) || !ShortID.IsShortID(root))
                        {
                            WriteError(typeof(ArgumentException),
                                "The value for DescendantOf criteria must be an Item or ID.",
                                ErrorIds.InvalidOperation, ErrorCategory.InvalidArgument, criteria.Value);
                            return null;
                        }

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i["_path"].Contains(root).Boost(boost), operation)
                            : predicate.AddPredicate(i => i["_path"].Contains(root).Boost(boost), operation);
                        break;
                    case FilterType.StartsWith:
                        var startsWith = ObjectToString(criteria.Value);

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].StartsWith(startsWith, comparer).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].StartsWith(startsWith, comparer).Boost(boost), operation);
                        break;
                    case FilterType.Contains:
                        if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                        {
                            WriteWarning("Case insensitive is not supported on Contains criteria due to platform limitations.");
                        }

                        var contains = ObjectToString(criteria.Value);

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].Contains(contains).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].Contains(contains).Boost(boost), operation);
                        break;
                    case FilterType.ContainsAny:
                        if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                        {
                            WriteWarning("Case insensitive is not supported on Contains criteria due to platform limitations.");
                        }

                        var valuesAny = ObjectToStringArray(criteria.Value);
                        predicate = criteria.Invert
                            ? predicate.AddPredicate(valuesAny.Aggregate(PredicateBuilder.True<T>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))))
                            : predicate.AddPredicate(valuesAny.Aggregate(PredicateBuilder.True<T>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))));
                        break;
                    case FilterType.ContainsAll:
                        if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                        {
                            WriteWarning("Case insensitive is not supported on Contains criteria due to platform limitations.");
                        }

                        var valuesAll = ObjectToStringArray(criteria.Value);
                        predicate = criteria.Invert
                            ? predicate.AddPredicate(valuesAll.Aggregate(PredicateBuilder.True<T>(), (current, keyword) => current.And(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))))
                            : predicate.AddPredicate(valuesAll.Aggregate(PredicateBuilder.True<T>(), (current, keyword) => current.And(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))));
                        break;
                    case FilterType.EndsWith:
                        var endsWith = ObjectToString(criteria.Value);

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].EndsWith(endsWith, comparer).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].EndsWith(endsWith, comparer).Boost(boost), operation);
                        break;
                    case FilterType.Equals:
                        var equals = ObjectToString(criteria.Value);

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].Equals(@equals, comparer).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].Equals(@equals, comparer).Boost(boost), operation);
                        break;
                    case FilterType.Fuzzy:
                        var fuzzy = ObjectToString(criteria.Value);

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].Like(fuzzy).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].Like(fuzzy).Boost(boost), operation);
                        break;
                    case FilterType.InclusiveRange:
                    case FilterType.ExclusiveRange:
                        predicate = GetRangeExpression(predicate, criteria, operation);
                        break;
                    case FilterType.MatchesRegex:
                        var regex = criteria.StringValue;

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].Matches(regex).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].Matches(regex).Boost(boost), operation);
                        break;
                    case FilterType.MatchesWildcard:
                        var wildcard = criteria.StringValue;

                        predicate = criteria.Invert
                            ? predicate.AddPredicate(i => !i[criteria.Field].MatchWildcard(wildcard).Boost(boost), operation)
                            : predicate.AddPredicate(i => i[criteria.Field].MatchWildcard(wildcard).Boost(boost), operation);
                        break;
                    case FilterType.GreaterThan:
                    case FilterType.LessThan:
                        predicate = GetComparisonExpression(predicate, criteria, operation);
                        break;
                }
            }

            return predicate;
        }

        private static Expression<Func<T, bool>> GetRangeExpression<T>(Expression<Func<T, bool>> predicate, SearchCriteria criteria, SearchOperation operation) where T : ISearchResult
        {
            var inclusion = (criteria.Filter == FilterType.InclusiveRange)
                ? Inclusion.Both
                : Inclusion.None;

            var boost = criteria.Boost;
            var value = criteria.Value;
            if (value is object[])
            {
                switch (value)
                {
                    case string[] s:
                        value = s;
                        break;
                    case DateTime[] datetime:
                        value = datetime;
                        break;
                    case double[] d:
                        value = d;
                        break;
                    case int[] i:
                        value = i;
                        break;
                }
            }

            switch (value)
            {
                case string[] _:
                    var pairString = (string[])value;
                    var leftString = pairString[0];
                    var rightString = pairString[1];
                    predicate = criteria.Invert
                        ? predicate.AddPredicate(i => !i[criteria.Field].Between(leftString, rightString, inclusion).Boost(boost), operation)
                        : predicate.AddPredicate(i => i[criteria.Field].Between(leftString, rightString, inclusion).Boost(boost), operation);
                    break;
                case DateTime[] _:
                    var pairDateTime = (DateTime[])value;
                    var leftDateTime = pairDateTime[0];
                    var rightDateTime = pairDateTime[1];
                    predicate = criteria.Invert
                        ? predicate.AddPredicate(i => !((DateTime)i[(ObjectIndexerKey)criteria.Field]).Between(leftDateTime, rightDateTime, inclusion).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((DateTime)i[(ObjectIndexerKey)criteria.Field]).Between(leftDateTime, rightDateTime, inclusion).Boost(boost), operation);
                    break;
                case double[] _:
                    var pairDouble = (double[])value;
                    var leftDouble = pairDouble[0];
                    var rightDouble = pairDouble[1];
                    predicate = criteria.Invert
                        ? predicate.AddPredicate(i => !((double)i[(ObjectIndexerKey)criteria.Field]).Between(leftDouble, rightDouble, inclusion).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((double)i[(ObjectIndexerKey)criteria.Field]).Between(leftDouble, rightDouble, inclusion).Boost(boost), operation);
                    break;
                case int[] _:
                    var pairInt = (int[])value;
                    var leftInt = pairInt[0];
                    var rightInt = pairInt[1];
                    predicate = criteria.Invert
                        ? predicate.AddPredicate(i => !((int)i[(ObjectIndexerKey)criteria.Field]).Between(leftInt, rightInt, inclusion).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((int)i[(ObjectIndexerKey)criteria.Field]).Between(leftInt, rightInt, inclusion).Boost(boost), operation);
                    break;
            }

            return predicate;
        }

        private static Expression<Func<T, bool>> GetComparisonExpression<T>(Expression<Func<T, bool>> predicate, SearchCriteria criteria, SearchOperation operation) where T : ISearchResult
        {
            var boost = criteria.Boost;
            var value = criteria.Value;
            
            switch (value)
            {
                case DateTime _:
                    var compareDateTime = (DateTime)value;
                    predicate = (criteria.Invert || criteria.Filter == FilterType.LessThan)
                        ? predicate.AddPredicate(i => ((DateTime)i[(ObjectIndexerKey)criteria.Field] < compareDateTime).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((DateTime)i[(ObjectIndexerKey)criteria.Field] > compareDateTime).Boost(boost), operation);
                    break;
                case double _:
                    var compareDouble = (double)value;
                    predicate = (criteria.Invert || criteria.Filter == FilterType.LessThan)
                        ? predicate.AddPredicate(i => ((double)i[(ObjectIndexerKey)criteria.Field] < compareDouble).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((double)i[(ObjectIndexerKey)criteria.Field] > compareDouble).Boost(boost), operation);
                    break;
                case int _:
                    var compareInt = (int)value;
                    predicate = (criteria.Invert || criteria.Filter == FilterType.LessThan)
                        ? predicate.AddPredicate(i => ((int)i[(ObjectIndexerKey)criteria.Field] < compareInt).Boost(boost), operation)
                        : predicate.AddPredicate(i => ((int)i[(ObjectIndexerKey)criteria.Field] > compareInt).Boost(boost), operation);
                    break;
            }

            return predicate;
        }

        private static string ObjectToString(object value)
        {
            string convertedValue;

            switch (value)
            {
                case Item item:
                    convertedValue = item.ID.ToString();
                    break;
                default:
                    convertedValue = value.ToString();
                    break;
            }

            if (ID.IsID(convertedValue))
            {
                convertedValue = ID.Parse(convertedValue).ToShortID().ToString().ToLower();
            }

            return convertedValue;
        }

        private static IEnumerable<string> ObjectToStringArray(object value)
        {
            string[] values = null;
            switch (value)
            {
                case string[] _:
                    return (string[])value;
                case Item[] _:
                    values = ((Item[])value).Select(x => x.ID.ToString()).ToArray();
                    break;
                case object[] _:
                    if (value is PSObject[] items)
                    {
                        values = items.Select(x => ((Item)x.BaseObject).ID.ToString()).ToArray();
                    }
                    else
                    {
                        values = Array.ConvertAll((object[])value, x => x.ToString());
                    }
                    break;
                case ArrayList _:
                    values = Array.ConvertAll(((ArrayList)value).ToArray(), x => x.ToString());
                    break;
                case List<string> _:
                    values = ((List<string>)value).ToArray();
                    break;
            }

            if (values != null)
            {
                for (var i = 0; i < values.Length; i++)
                {
                    if (!ID.IsID(values[i])) continue;

                    var id = values[i];
                    values[i] = ID.Parse(id).ToShortID().ToString().ToLower();
                }
            }

            return values;
        }

        public static IQueryable<T> ProcessScopeQuery<T>(IQueryable<T> query, IProviderSearchContext context, string scope) where T : ISearchResult
        {
            var searchStringModel = SearchStringModel.ParseDatasourceString(scope).ToList();
            query = LinqHelper.CreateQuery<T>(context, searchStringModel);
            query = AddSorting(query, searchStringModel);
            return query;
        }

        private static IQueryable<T> AddSorting<T>(IQueryable<T> query, IEnumerable<SearchStringModel> model) where T : ISearchResult
        {
            foreach (var searchStringModel in model.Where(m => m.Type == "sort"))
            {
                var isDesc = searchStringModel.Value.EndsWith("[desc]", StringComparison.OrdinalIgnoreCase);
                var key = isDesc ? searchStringModel.Value.Substring(0, searchStringModel.Value.Length - "[desc]".Length).Trim() : searchStringModel.Value.Trim();
                query = query.OrderBy(key);
            }
            return query;
        }

        internal static IQueryable<T> GetQueryable<T>(T queryableObject, IProviderSearchContext searchContext) where T : ISearchResult
        {
            return searchContext.GetQueryable<T>();
        }

        internal static IQueryable<T> ApplyWhereAndValues<T>(IQueryable<T> query, string whereCondition, object[] whereValues) where T : ISearchResult
        {
            return query.Where(whereCondition, whereValues.BaseArray());
        }

        internal static IQueryable<T> ApplyWhere<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate) where T : ISearchResult
        {
            return query.Where(predicate);
        }

        internal static IQueryable<T> ApplyFilter<T>(IQueryable<T> query, Expression<Func<T, bool>> predicate) where T : ISearchResult
        {
            return query.Filter(predicate);
        }

        internal static IQueryable<T> ApplyFilterAndValues<T>(IQueryable<T> query, string filterCondition, object[] filterValues) where T : ISearchResult
        {
            return query.Filter(filterCondition, filterValues.BaseArray());
        }

        public static Expression<Func<T, string>> MemberSelector<T>(IQueryable<T> query, string name) where T : ISearchResult
        {
            var parameter = Expression.Parameter(typeof(T), "item");
            var body = Expression.PropertyOrField(parameter, name);
            return Expression.Lambda<Func<T, string>>(body, parameter);
        }

        internal static FacetResults ApplyFacetOn<T>(IQueryable<T> query, string[] facetFields, int minimumResultCount) where T : ISearchResult
        {
            foreach (var facetField in facetFields)
            {
                var facetExpression = MemberSelector(query, facetField);
                query = query.FacetOn(facetExpression, minimumResultCount);
            }

            return query.GetFacets();
        }

        internal static Expression<Func<T, bool>> GetPredicateBuilder<T>(T queryableObject, bool shouldOr) where T : ISearchResult
        {
            return shouldOr
                ? PredicateBuilder.False<T>()
                : PredicateBuilder.True<T>();
        }

        internal static Expression<Func<T, bool>> GetPredicateAndOr<T>(Expression<Func<T, bool>> predicate, Expression<Func<T, bool>> joinedPredicate, bool shouldOr) where T : ISearchResult
        {
            return shouldOr
                ? predicate.Or(joinedPredicate)
                : predicate.And(joinedPredicate);
        }

        internal static IQueryable<T> ApplyOrderBy<T>(IQueryable<T> query, string orderBy) where T : ISearchResult
        {
            return query.OrderBy(orderBy);
        }

        internal static List<object> ApplySelect<T>(IQueryable<T> query, string[] properties)
            where T : ISearchResult
        {
            return query.SelectProperties(properties).Cast<object>().ToList();
        }

        internal static IQueryable<T> ApplySkipAndTake<T>(IQueryable<T> query, int first, int skip)
            where T : ISearchResult

        {
            if (skip > 0)
            {
                query = query.Skip(skip);
            }

            if (first > 0)
            {
                query = query.Take(first);
            }

            return query;
        }

        internal static List<T> ApplySkipAndTakeByPosition<T>(IQueryable<T> query, int first, int last, int skip) where T : ISearchResult
        {
            var count = query.Count();
            var skipEnd = (last != 0 && first == 0);
            var skipFirst = skipEnd ? 0 : skip;
            var takeFirst = first;
            if (last == 0 && first == 0)
            {
                takeFirst = count - skipFirst;
            }
            var firstObjects = query.Skip(skipFirst).Take(takeFirst);
            var takenAndSkipped = (skipFirst + takeFirst);
            if (takenAndSkipped >= count || last == 0 || (skipEnd && skip >= (count - takenAndSkipped)))
            {
                return firstObjects.ToList();
            }
            var takeAndSkipAtEnd = last + (skipEnd ? skip : 0);
            var skipBeforeEnd = count - takenAndSkipped - takeAndSkipAtEnd;
            var takeLast = last;
            if (skipBeforeEnd >= 0)
            {
                // Concat not support by Sitecore.
                return firstObjects.ToList().Concat(query.Skip(takenAndSkipped + skipBeforeEnd).Take(takeLast)).ToList();
            }
            takeLast += skipBeforeEnd;
            skipBeforeEnd = 0;
            // Concat not support by Sitecore.
            return firstObjects.ToList().Concat(query.Skip(takenAndSkipped + skipBeforeEnd).Take(takeLast)).ToList();
        }
    }
}