using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Find, "Item")]
    [OutputType(typeof (Item))]
    public class FindItemCommand : BaseCommand, IDynamicParameters
    {
        private readonly string[] indexes = ContentSearchManager.Indexes.Select(i => i.Name).ToArray();

        public FindItemCommand()
        {
            AddDynamicParameter<string>("Index", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = true,
                Position = 0
            }, new ValidateSetAttribute(indexes));
        }

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
            string index;
            if (!TryGetParameter("Index", out index))
            {
                index = "sitecore_master_index";
            }
            using (var context = ContentSearchManager.GetIndex(index).CreateSearchContext())
            {
                // get all items in medialibrary
                var query = context.GetQueryable<SearchResultItem>();

                if (!String.IsNullOrEmpty(Where))
                {
                    query = query.Where(Where, WhereValues.BaseArray());
                }
                if (Criteria != null)
                    foreach (var filter in Criteria)
                    {
                        var criteria = filter;
                        var comparer = criteria.CaseSensitive
                            ? StringComparison.Ordinal
                            : StringComparison.OrdinalIgnoreCase;
                        switch (criteria.Filter)
                        {
                            case (FilterType.StartsWith):
                                query = query.Where(i => i[criteria.Field].StartsWith(criteria.Value, comparer));
                                break;
                            case (FilterType.Contains):
                                query = query.Where(i => i[criteria.Field].IndexOf(criteria.Value, 0, comparer) > -1);
                                break;
                            case (FilterType.EndsWith):
                                query = query.Where(i => i[criteria.Field].EndsWith(criteria.Value, comparer));
                                break;
                            case (FilterType.Equals):
                                query = query.Where(i => i[criteria.Field].Equals(criteria.Value, comparer));
                                break;
                        }
                    }
                if (!String.IsNullOrEmpty(OrderBy))
                {
                    query = query.OrderBy(OrderBy);
                }

                WriteObject(FilterByPosition(query), true);
            }
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
        EndsWith
    }

    public class SearchCriteria
    {
        public FilterType Filter { get; set; }
        public string Field { get; set; }
        public string Value { get; set; }
        public bool CaseSensitive { get; set; }
    }
}