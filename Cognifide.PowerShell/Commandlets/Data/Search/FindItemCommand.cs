using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Validation;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
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

        [AutocompleteSet("Indexes")]
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
                IQueryable<SearchResultItem>[] query = {context.GetQueryable<SearchResultItem>()};

                if (!string.IsNullOrEmpty(Where))
                {
                    SitecoreVersion.V75.OrNewer(() =>
                    {
                        query[0] = query[0].Where(Where, WhereValues.BaseArray());
                    }).ElseWriteWarning(this, nameof(Where), true);
                }
                if (Criteria != null)
                    foreach (var filter in Criteria)
                    {
                        var criteria = filter;
                        if(criteria.Value == null) continue;

                        var comparer = criteria.CaseSensitive.HasValue && criteria.CaseSensitive.Value
                            ? StringComparison.Ordinal
                            : StringComparison.OrdinalIgnoreCase;
                        switch (criteria.Filter)
                        {
                            case (FilterType.DescendantOf):
                                var ancestorId = string.Empty;
                                if (criteria.Value is Item)
                                {
                                    ancestorId = ((Item) criteria.Value).ID.ToShortID().ToString();
                                }
                                if (ID.IsID(criteria.Value.ToString()))
                                {
                                    ancestorId = ((ID) criteria.Value).ToShortID().ToString();
                                }
                                if (string.IsNullOrEmpty(ancestorId))
                                {
                                    WriteError(typeof(ArgumentException), "The root for DescendantOf criteria has to be an Item or an ID.", ErrorIds.InvalidOperation, ErrorCategory.InvalidArgument, criteria.Value);
                                    return;
                                }
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i["_path"].Contains(ancestorId))
                                    : query[0].Where(i => i["_path"].Contains(ancestorId));
                                break;
                            case (FilterType.StartsWith):
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i[criteria.Field].StartsWith(criteria.StringValue, comparer))
                                    : query[0].Where(i => i[criteria.Field].StartsWith(criteria.StringValue, comparer));
                                break;
                            case (FilterType.Contains):
                                if (comparer == StringComparison.OrdinalIgnoreCase && criteria.CaseSensitive.HasValue)
                                {
                                    WriteWarning(
                                        "Case insensitiveness is not supported on Contains criteria due to platform limitations.");
                                }
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i[criteria.Field].Contains(criteria.StringValue))
                                    : query[0].Where(i => i[criteria.Field].Contains(criteria.StringValue));
                                break;
                            case (FilterType.EndsWith):
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => i[criteria.Field].EndsWith(criteria.StringValue, comparer))
                                    : query[0].Where(i => i[criteria.Field].EndsWith(criteria.StringValue, comparer));
                                break;
                            case (FilterType.Equals):
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i[criteria.Field].Equals(criteria.StringValue, comparer))
                                    : query[0].Where(i => i[criteria.Field].Equals(criteria.StringValue, comparer));
                                break;
                            case (FilterType.Fuzzy):
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i[criteria.Field].Like(criteria.StringValue))
                                    : query[0].Where(i => i[criteria.Field].Like(criteria.StringValue));
                                break;
                            case (FilterType.InclusiveRange):
                            case (FilterType.ExclusiveRange):
                                if (!(criteria.Value is string[])) break;

                                var inclusion = (criteria.Filter == FilterType.InclusiveRange) ? Inclusion.Both : Inclusion.None;

                                var pair = (object[])criteria.Value;
                                var left = (pair[0] is DateTime) ? ((DateTime)pair[0]).ToString("yyyyMMdd") : pair[0].ToString();
                                var right = (pair[1] is DateTime) ? ((DateTime)pair[1]).ToString("yyyyMMdd") : pair[1].ToString();
                                query[0] = criteria.Invert
                                    ? query[0].Where(i => !i[criteria.Field].Between(left, right, inclusion))
                                    : query[0].Where(i => i[criteria.Field].Between(left, right, inclusion));
                                break;
                        }
                    }

                if (!string.IsNullOrEmpty(OrderBy))
                {
                    SitecoreVersion.V75.OrNewer(() =>
                    {
                        query[0] = OrderIfSupported(query[0]);
                    }).ElseWriteWarning(this, nameof(OrderBy), true);
                }

                WriteObject(FilterByPosition(query[0]), true);
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
        ExclusiveRange
    }

    public class SearchCriteria
    {
        public FilterType Filter { get; set; }
        public string Field { get; set; }
        public object Value { get; set; }
        public bool? CaseSensitive { get; set; }
        internal string StringValue { get { return Value.ToString(); } }
        public bool Invert { get; set; }
    }
}