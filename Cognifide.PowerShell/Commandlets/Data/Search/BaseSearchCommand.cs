using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using System.Web;
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
            var shouldOr = operation == SearchOperation.Or;
            var predicate = shouldOr
                ? PredicateBuilder.False<SearchResultItem>()
                : PredicateBuilder.True<SearchResultItem>();

            if (criterias != null)
            {
                foreach (var filter in criterias)
                {
                    var criteria = filter;
                    if (criteria.Value == null) continue;

                    var comparer = criteria.CaseSensitive.HasValue && criteria.CaseSensitive.Value
                        ? StringComparison.Ordinal
                        : StringComparison.OrdinalIgnoreCase;
                    switch (criteria.Filter)
                    {
                        case (FilterType.DescendantOf):
                            var ancestorId = string.Empty;
                            if (criteria.Value is Item)
                            {
                                ancestorId = ((Item)criteria.Value).ID.ToShortID().ToString();
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
                                ? predicate.AddPredicate(i => !i["_path"].Contains(ancestorId), shouldOr)
                                : predicate.AddPredicate(i => i["_path"].Contains(ancestorId), shouldOr);
                            break;
                        case (FilterType.StartsWith):
                            var startsWith = criteria.StringValue;
                            if (ID.IsID(startsWith))
                            {
                                startsWith = ID.Parse(startsWith).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].StartsWith(startsWith, comparer), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].StartsWith(startsWith, comparer), shouldOr);
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
                                ? predicate.AddPredicate(i => !i[criteria.Field].Contains(contains), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].Contains(contains), shouldOr);
                            break;
                        case (FilterType.EndsWith):
                            var endsWith = criteria.StringValue;
                            if (ID.IsID(endsWith))
                            {
                                endsWith = ID.Parse(endsWith).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].EndsWith(endsWith, comparer), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].EndsWith(endsWith, comparer), shouldOr);
                            break;
                        case (FilterType.Equals):
                            var equals = criteria.StringValue;
                            if (ID.IsID(equals))
                            {
                                equals = ID.Parse(equals).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Equals(equals, comparer), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].Equals(equals, comparer), shouldOr);
                            break;
                        case (FilterType.Fuzzy):
                            var fuzzy = criteria.StringValue;
                            if (ID.IsID(fuzzy))
                            {
                                fuzzy = ID.Parse(fuzzy).ToShortID().ToString().ToLower();
                            }

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Like(fuzzy), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].Like(fuzzy), shouldOr);
                            break;
                        case (FilterType.InclusiveRange):
                        case (FilterType.ExclusiveRange):
                            if (!(criteria.Value is string[])) break;

                            var inclusion = (criteria.Filter == FilterType.InclusiveRange)
                                ? Inclusion.Both
                                : Inclusion.None;

                            var pair = (object[])criteria.Value;
                            var left = (pair[0] as DateTime?)?.ToString("yyyyMMdd") ?? pair[0].ToString();
                            var right = (pair[1] as DateTime?)?.ToString("yyyyMMdd") ?? pair[1].ToString();

                            predicate = criteria.Invert
                                ? predicate.AddPredicate(i => !i[criteria.Field].Between(left, right, inclusion), shouldOr)
                                : predicate.AddPredicate(i => i[criteria.Field].Between(left, right, inclusion), shouldOr);
                            break;
                    }
                }
            }

            return predicate;
        }
    }
}