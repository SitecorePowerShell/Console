using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    public class BaseSearchCommand : BaseCommand
    {
        public Expression<Func<SearchResultItem, bool>> ProcessCriteria(SearchCriteria[] criterias, SearchOperation operation)
        {
            var predicate = operation == SearchOperation.Or
                ? PredicateBuilder.False<SearchResultItem>()
                : PredicateBuilder.True<SearchResultItem>();

            if (criterias != null)
            {
                foreach (var criteria in criterias)
                {
                    if (criteria.Value == null) continue;
                    var boost = criteria.Boost;
                    var comparer = criteria.CaseSensitive.HasValue && criteria.CaseSensitive.Value
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase;
                    switch (criteria.Filter)
                    {
                        case (FilterType.DescendantOf):
                            var ancestorId = string.Empty;
                            if (criteria.Value is Item item)
                            {
                                ancestorId = item.ID.ToShortID().ToString();
                            }
                            else if (ID.IsID(criteria.Value.ToString()))
                            {
                                ancestorId = ((ID)criteria.Value).ToShortID().ToString().ToLower();
                            }

                            if (string.IsNullOrEmpty(ancestorId))
                            {
                                WriteError(typeof(ArgumentException),
                                    "The root for DescendantOf criteria has to be an Item or an ID.",
                                    ErrorIds.InvalidOperation, ErrorCategory.InvalidArgument, criteria.Value);
                                return null;
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i["_path"].Contains(ancestorId).Boost(boost), operation)
                                : predicate.AddPredicate(i => i["_path"].Contains(ancestorId).Boost(boost), operation);
                            break;
                        case (FilterType.StartsWith):
                            var startsWith = criteria.StringValue;
                            if (ID.IsID(startsWith))
                            {
                                startsWith = ID.Parse(startsWith).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].StartsWith(startsWith, comparer).Boost(boost), operation)
                                : predicate.AddPredicate(i => i[criteria.Field].StartsWith(startsWith, comparer).Boost(boost), operation);
                            break;
                        case (FilterType.Contains):
                            if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                            {
                                WriteWarning("Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                            }

                            var contains = criteria.StringValue;
                            if (ID.IsID(contains))
                            {
                                contains = ID.Parse(contains).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Contains(contains).Boost(boost), operation)
                                : predicate.AddPredicate(i => i[criteria.Field].Contains(contains).Boost(boost), operation);
                            break;
                        case FilterType.ContainsAny:
                            if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                            {
                                WriteWarning("Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                            }

                            var valuesAny = ObjectToStringArray(criteria.Value);
                            predicate = criteria.Invert
                                ? predicate.AddPredicate(valuesAny.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))))
                                : predicate.AddPredicate(valuesAny.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))));
                            break;
                        case FilterType.ContainsAll:
                            if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                            {
                                WriteWarning("Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                            }

                            var valuesAll = ObjectToStringArray(criteria.Value);
                            predicate = criteria.Invert
                                ? predicate.AddPredicate(valuesAll.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.And(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))))
                                : predicate.AddPredicate(valuesAll.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.And(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Contains(keyword))));
                            break;
                        case (FilterType.EndsWith):
                            var endsWith = criteria.StringValue;
                            if (ID.IsID(endsWith))
                            {
                                endsWith = ID.Parse(endsWith).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].EndsWith(endsWith, comparer).Boost(boost), operation)
                                : predicate.AddPredicate(i => i[criteria.Field].EndsWith(endsWith, comparer).Boost(boost), operation);
                            break;
                        case (FilterType.Equals):
                            var equals = criteria.StringValue;
                            if (ID.IsID(equals))
                            {
                                equals = ID.Parse(equals).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Equals(equals, comparer).Boost(boost), operation)
                                : predicate.AddPredicate(i => i[criteria.Field].Equals(equals, comparer).Boost(boost), operation);
                            break;
                        case (FilterType.Fuzzy):
                            var fuzzy = criteria.StringValue;
                            if (ID.IsID(fuzzy))
                            {
                                fuzzy = ID.Parse(fuzzy).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Like(fuzzy).Boost(boost), operation)
                                : predicate.AddPredicate(i => i[criteria.Field].Like(fuzzy).Boost(boost), operation);
                            break;
                        case (FilterType.InclusiveRange):
                        case (FilterType.ExclusiveRange):
                            predicate = GetRangeExpression(predicate, criteria, operation);
                            break;
                    }
                }
            }

            return predicate;
        }

        private static Expression<Func<SearchResultItem, bool>> GetRangeExpression(Expression<Func<SearchResultItem, bool>> predicate, SearchCriteria criteria, SearchOperation operation)
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
                    var leftDateTime = pairDateTime[0].ToString("yyyyMMdd");
                    var rightDateTime = pairDateTime[1].ToString("yyyyMMdd");
                    predicate = criteria.Invert
                        ? predicate.AddPredicate(i => !i[criteria.Field].Between(leftDateTime, rightDateTime, inclusion).Boost(boost), operation)
                        : predicate.AddPredicate(i => i[criteria.Field].Between(leftDateTime, rightDateTime, inclusion).Boost(boost), operation);
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
    }
}