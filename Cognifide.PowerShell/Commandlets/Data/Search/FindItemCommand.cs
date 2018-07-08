using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Validation;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data.Search
{
    [Cmdlet(VerbsCommon.Find, "Item")]
    [OutputType(typeof(SearchResultItem))]
    public class FindItemCommand : BaseCommand
    {
        public static string[] Indexes
        {
            get { return ContentSearchManager.Indexes.Select(i => i.Name).ToArray(); }
        }

        [AutocompleteSet(nameof(Indexes))]
        [Parameter(Mandatory = true, Position = 0)]
        public string Index { get; set; }

        [Parameter]
        public SearchCriteria[] Criteria { get; set; }

        [Parameter]
        public string Where { get; set; }

        [Parameter]
        public object[] WhereValues { get; set; }

        [Parameter]
        public string OrderBy { get; set; }

        [Parameter]
        public int First { get; set; }

        [Parameter]
        public int Last { get; set; }

        [Parameter]
        public int Skip { get; set; }

        protected override void EndProcessing()
        {
            string index = string.IsNullOrEmpty(Index) ? "sitecore_master_index" : Index;

            using (var context = ContentSearchManager.GetIndex(index).CreateSearchContext())
            {
                // get all items in medialibrary
                IQueryable<SearchResultItem> query = context.GetQueryable<SearchResultItem>();

                

                if (!string.IsNullOrEmpty(Where))
                {
                    SitecoreVersion.V75.OrNewer(() =>
                    {
                        query = query.Where(Where, WhereValues.BaseArray());
                    }).ElseWriteWarning(this, nameof(Where), true);
                }
                if (Criteria != null)
                {
                    Expression<Func<SearchResultItem, bool>> expression = PredicateBuilder.True<SearchResultItem>();
                    foreach (var filter in Criteria)
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
                                    WriteError(typeof(ArgumentException), "The root for DescendantOf criteria has to be an Item or an ID.", ErrorIds.InvalidOperation, ErrorCategory.InvalidArgument, criteria.Value);
                                    return;
                                }

                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i.Path.Contains(ancestorId))
                                   : expression.And(i => i.Path.Contains(ancestorId));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i.Path.Contains(ancestorId))
                                   : expression.Or(i => i.Path.Contains(ancestorId));
                                        break;
                                    default:
                                        break;
                                }
                               
                                break;
                            case (FilterType.StartsWith):
                                var startsWith = criteria.StringValue;
                                if (ID.IsID(startsWith))
                                {
                                    startsWith = ID.Parse(startsWith).ToShortID().ToString().ToLower();
                                }

                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].StartsWith(startsWith, comparer))
                                   : expression.And(i => i[criteria.Field].StartsWith(startsWith, comparer));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].StartsWith(startsWith, comparer))
                                   : expression.Or(i => i[criteria.Field].StartsWith(startsWith, comparer));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.Contains):
                                if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                                {
                                    WriteWarning(
                                        "Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                                }
                                var contains = criteria.StringValue;
                                if (ID.IsID(contains))
                                {
                                    contains = ID.Parse(contains).ToShortID().ToString().ToLower();
                                }
                                
                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].Contains(contains))
                                   : expression.And(i => i[criteria.Field].Contains(contains));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].Contains(contains))
                                   : expression.Or(i => i[criteria.Field].Contains(contains));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.ContainsAny):
                                if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                                {
                                    WriteWarning(
                                        "Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                                }
                                List<string> values = new List<string>();
                                if (criteria.Value is Item[])
                                {
                                    Item[] items = (Item[])criteria.Value;
                                    values = items.Select(x => x.ID.ToShortID().ToString().ToLowerInvariant()).ToList();
                                }
                                else if (criteria.Value is string)
                                {
                                    string str = (string)criteria.Value;
                                    values = str.Split('|').ToList();
                                }
                                else if (criteria.Value is List<string>)
                                {
                                    values = (List<string>)criteria.Value;
                                }
                                else if (criteria.Value is PSObject[])
                                {
                                    PSObject[] items = (PSObject[])criteria.Value;
                                    values = items.Select(x => ((Item)x.BaseObject).ID.ToShortID().ToString().ToLowerInvariant()).ToList();
                                }
                                
                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(values.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))))
                                   : expression.And(values.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(values.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))))
                                   : expression.Or(values.Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.ContainsAll):
                                if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                                {
                                    WriteWarning(
                                        "Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                                }
                                
                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(((List<string>)criteria.Value).Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))))
                                   : expression.And(((List<string>)criteria.Value).Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(((List<string>)criteria.Value).Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => !((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))))
                                   : expression.Or(((List<string>)criteria.Value).Aggregate(PredicateBuilder.True<SearchResultItem>(), (current, keyword) => current.Or(c => ((string)c[(ObjectIndexerKey)criteria.Field]).Equals(keyword))));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.EndsWith):
                                var endsWith = criteria.StringValue;
                                if (ID.IsID(endsWith))
                                {
                                    endsWith = ID.Parse(endsWith).ToShortID().ToString().ToLower();
                                }

                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].EndsWith(endsWith, comparer))
                                   : expression.And(i => i[criteria.Field].EndsWith(endsWith, comparer));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].EndsWith(endsWith, comparer))
                                   : expression.Or(i => i[criteria.Field].EndsWith(endsWith, comparer));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.Equals):
                                var equals = criteria.StringValue;
                                if (ID.IsID(equals))
                                {
                                    equals = ID.Parse(equals).ToShortID().ToString().ToLower();
                                }
                                
                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].Equals(equals, comparer))
                                   : expression.And(i => i[criteria.Field].Equals(equals, comparer));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].Equals(equals, comparer))
                                   : expression.Or(i => i[criteria.Field].Equals(equals, comparer));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.Fuzzy):
                                var fuzzy = criteria.StringValue;
                                if (ID.IsID(fuzzy))
                                {
                                    fuzzy = ID.Parse(fuzzy).ToShortID().ToString().ToLower();
                                }

                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].Like(fuzzy))
                                   : expression.And(i => i[criteria.Field].Like(fuzzy));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].Like(fuzzy))
                                   : expression.Or(i => i[criteria.Field].Like(fuzzy));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                            case (FilterType.InclusiveRange):
                            case (FilterType.ExclusiveRange):
                                if (!(criteria.Value is string[])) break;

                                var inclusion = (criteria.Filter == FilterType.InclusiveRange) ? Inclusion.Both : Inclusion.None;

                                var pair = (object[])criteria.Value;
                                var left = (pair[0] as DateTime?)?.ToString("yyyyMMdd") ?? pair[0].ToString();
                                var right = (pair[1] as DateTime?)?.ToString("yyyyMMdd") ?? pair[1].ToString();
                                
                                switch (criteria.Condition)
                                {
                                    case ConditionType.And:
                                        expression = criteria.Invert
                                   ? expression.And(i => !i[criteria.Field].Between(left, right, inclusion))
                                   : expression.And(i => i[criteria.Field].Between(left, right, inclusion));
                                        break;
                                    case ConditionType.Or:
                                        expression = criteria.Invert
                                   ? expression.Or(i => !i[criteria.Field].Between(left, right, inclusion))
                                   : expression.Or(i => i[criteria.Field].Between(left, right, inclusion));
                                        break;
                                    default:
                                        break;
                                }
                                break;
                        }
                    }
                    query = query.Where(expression);
                }
                
                if (!string.IsNullOrEmpty(OrderBy))
                {
                    SitecoreVersion.V75.OrNewer(() =>
                    {
                        query = OrderIfSupported(query);
                    }).ElseWriteWarning(this, nameof(OrderBy), true);
                }

                WriteObject(FilterByPosition(query), true);
            }
        }

        private IQueryable<SearchResultItem> OrderIfSupported(IQueryable<SearchResultItem> query)
        {
            query = query.OrderBy(OrderBy);
            return query;
        }

        private List<SearchResultItem> FilterByPosition(IQueryable<SearchResultItem> query)
        {
            var count = query.Count();
            var skipEnd = (Last != 0 && First == 0);
            var skipFirst = skipEnd ? 0 : Skip;
            var takeFirst = First;
            if (Last == 0 && First == 0)
            {
                takeFirst = count - skipFirst;
            }
            var firstObjects = query.Skip(skipFirst).Take(takeFirst);
            var takenAndSkipped = (skipFirst + takeFirst);
            if (takenAndSkipped >= count || Last == 0 || (skipEnd && Skip >= (count - takenAndSkipped)))
            {
                return firstObjects.ToList();
            }
            var takeAndSkipAtEnd = Last + (skipEnd ? Skip : 0);
            var skipBeforeEnd = count - takenAndSkipped - takeAndSkipAtEnd;
            var takeLast = Last;
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

    public enum FilterType
    {
        None,
        Equals,
        StartsWith,
        Contains,
        EndsWith,
        DescendantOf,
        Fuzzy,
        InclusiveRange,
        ExclusiveRange,
        ContainsAny,
        ContainsAll
    }
    public enum ConditionType
    {
        And,
        Or
    }

    public class SearchCriteria
    {
        public FilterType Filter { get; set; }
        public ConditionType Condition { get; set; }
        public string Field { get; set; }
        public object Value { get; set; }
        public bool? CaseSensitive { get; set; }
        internal string StringValue { get { return Value.ToString(); } }
        public bool Invert { get; set; }

        public SearchCriteria NestedCriterias { get; set; }
    }
}