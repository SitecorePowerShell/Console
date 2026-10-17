using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq.Extensions;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Utilities;
using Spe.Core.Extensions;
using Spe.Core.Utility;
using Spe.Core.Validation;

namespace Spe.Commands.Data.Search
{
    [Cmdlet(VerbsCommon.Find, "Item", DefaultParameterSetName = "Criteria")]
    [OutputType(typeof(SearchResultItem))]
    public class FindItemCommand : BaseSearchCommand
    {
        public static string[] Indexes
        {
            get { return ContentSearchManager.Indexes.Select(i => i.Name).ToArray(); }
        }

        [AutocompleteSet(nameof(Indexes))]
        [Parameter(Position = 0)]
        public string Index { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        public SearchCriteria[] Criteria { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public string Where { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public object[] WhereValues { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Dynamic")]
        public object[] FilterValues { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        [Parameter(ParameterSetName = "Dynamic")]
        [Parameter(ParameterSetName = "Predicate")]
        [Parameter(ParameterSetName = "ScopeQuery")]
        [Parameter(ParameterSetName = "SearchStringModel")]
        public string[] FacetOn { get; set; }

        [Parameter(ParameterSetName = "Criteria")]
        [Parameter(ParameterSetName = "Dynamic")]
        [Parameter(ParameterSetName = "Predicate")]
        [Parameter(ParameterSetName = "ScopeQuery")]
        [Parameter(ParameterSetName = "SearchStringModel")]
        public int FacetMinCount { get; set; }

        [Parameter]
        public Type QueryType { get; set; }

        [Alias("Predicate")]
        [Parameter(ParameterSetName = "Predicate")]
        public dynamic WherePredicate { get; set; }

        [Parameter(ParameterSetName = "Predicate")]
        public dynamic FilterPredicate { get; set; }

        [Parameter(ParameterSetName = "ScopeQuery")]
        public string ScopeQuery { get; set; }

        [Parameter(ParameterSetName = "SearchStringModel")]
        public SearchStringModel[] SearchStringModels { get; set; }

        [Parameter]
        public string OrderBy { get; set; }

        [Parameter]
        public int First { get; set; }

        [Parameter]
        public int Last { get; set; }

        [Parameter]
        public int Skip { get; set; }

        [Parameter]
        public string[] Property { get; set; }
        
        [Parameter]
        [AutocompleteSet(nameof(Templates))]
        public virtual string[] Template { get; set; }

        public static string[] Templates => MiscAutocompleteSets.Templates;
        
        [Parameter]
        public virtual SwitchParameter LatestVersion { get; set; }
        
        private readonly List<SearchCriteria> _helperCriteria = new List<SearchCriteria>();
        private List<string> _templateIds = new List<string>();
        
        protected override void BeginProcessing()
        {
            var drive = Path.IsNullOrWhiteSpace() ? "master" : PathUtilities.GetDrive(Path, SessionState?.Drive?.Current?.Name ?? "master");
            if (Template != null && Template.Any())
            {
                _templateIds = Template.Select(templateName => TemplateUtils.GetFromPath(templateName, drive).ID.ToString()).ToList();
            }
            
            if (!Path.IsNullOrWhiteSpace())
            {
                var rootItem = PathUtilities.GetItem(Path, SessionState?.Drive?.Current?.Name, SessionState?.Drive?.Current?.CurrentLocation);
                if (rootItem != null)
                {
                    _helperCriteria.Add(new SearchCriteria()
                    {
                        Field = "_path",
                        Filter = FilterType.Contains,
                        Value = rootItem.ID
                    });
                }
            }

            if (LatestVersion)
            {
                _helperCriteria.Add(new SearchCriteria()
                {
                    Field = "_latestversion",
                    Filter = FilterType.Equals,
                    Value = "1"
                });
                
            }
        }
        
        protected override void EndProcessing()
        {
            var drive = Path.IsNullOrWhiteSpace() ? "master" : PathUtilities.GetDrive(Path, SessionState?.Drive?.Current?.Name ?? "master");
            var index = !Index.IsNullOrWhiteSpace() ? Index : $"sitecore_{drive}_index";

            var searchIndex = ContentSearchManager.GetIndex(index);
            using (var context = searchIndex.CreateSearchContext())
            {
                var queryableType = typeof(SearchResultItem);
                if (QueryType != null && QueryType != queryableType && 
                    (QueryType.IsSubclassOf(queryableType) || QueryType.ImplementsInterface(typeof(ISearchResult))))
                {
                    queryableType = QueryType;
                }

                var objType = (dynamic)Activator.CreateInstance(queryableType);
                var query = GetQueryable(objType, context);
                
                // Dynamic
                if (!string.IsNullOrEmpty(Where))
                {
                    query = ApplyWhereAndValues(query, Where, WhereValues);
                }
                else
                {
                    if (WhereValues != null)
                    {
                        WriteWarning($"The parameter ({nameof(Where)}) containing the dynamic Linq is missing.");
                    }
                }

                if (!string.IsNullOrEmpty(Filter))
                {
                    query = ApplyFilterAndValues(query, Filter, FilterValues);
                }
                else
                {
                    if (FilterValues != null)
                    {
                        WriteWarning($"The parameter ({nameof(Filter)}) containing the dynamic Linq is missing.");
                    }
                }
                // Dynamic End
                
                // Criteria
                if (Criteria != null)
                {
                    var criteriaPredicate = ProcessCriteria(objType, Criteria.ToArray(), SearchOperation.And);
                    query = ApplyWhere(query, criteriaPredicate);
                }
                // Criteria End
                
                // HelperCriteria
                if (_helperCriteria.Any())
                {
                    var criteriaPredicate = ProcessCriteria(objType, _helperCriteria.ToArray(), SearchOperation.And);
                    query = ApplyWhere(query, criteriaPredicate);
                }
                // HelperCriteria End

                // Template
                if (_templateIds.Any())
                {
                    // _templates (inherited templates) only exists when the AllTemplates computed field is enabled,
                    // vanilla indexes have just _template (the item's own template)
                    var hasAllTemplates = searchIndex.Configuration?.DocumentOptions?.ComputedIndexFields?
                        .Any(field => field.FieldName.Is("_templates")) == true;
                    var templateCriteria = _templateIds.Select(templateId => new SearchCriteria()
                    {
                        Field = hasAllTemplates ? "_templates" : "_template",
                        Filter = hasAllTemplates ? FilterType.Contains : FilterType.Equals,
                        Value = templateId
                    }).ToArray();
                    var templatePredicate = ProcessCriteria(objType, templateCriteria, SearchOperation.Or);
                    query = ApplyWhere(query, templatePredicate);
                }
                // Template End
                
                // Predicate
                if (WherePredicate != null)
                {
                    var boxedPredicate = WherePredicate;
                    if (boxedPredicate is PSObject)
                    {
                        boxedPredicate = (WherePredicate as PSObject)?.BaseObject;
                    }
                    query = ApplyWhere(query, boxedPredicate);
                }

                if (FilterPredicate != null)
                {
                    var boxedPredicate = FilterPredicate;
                    if (boxedPredicate is PSObject)
                    {
                        boxedPredicate = (FilterPredicate as PSObject)?.BaseObject;
                    }
                    query = ApplyFilter(query, boxedPredicate);
                }
                // Predicate End
                // ScopeQuery
                if (ScopeQuery != null)
                {
                    query = ProcessScopeQuery(query, context, ScopeQuery);
                }
                // ScopeQuery End
                // SearchStringModels
                if (SearchStringModels != null)
                {
                    query = ProcessSearchStringModels(query, context, SearchStringModels.ToList());
                }
                // SearchStringModels End

                if (FacetOn != null)
                {
                    var facets = ApplyFacetOn(query, FacetOn, FacetMinCount);
                    WriteObject(facets, true);
                    return;
                }

                if (!string.IsNullOrEmpty(OrderBy))
                {
                    query = ApplyOrderBy(query, OrderBy);
                }
                
                if (Property != null)
                {
                    // The use of Last is not supported because it requires Concat. Concat is not supported by Sitecore.
                    query = ApplySkipAndTake(query, First, Skip);
                    if (Last > 0)
                    {
                        WriteWarning($"The use of {nameof(Last)} is not supported when selecting with {nameof(Property)}.");
                    }

                    var queryableSelectProperties = ApplySelect(query, Property);
                    WriteObject(queryableSelectProperties, true);
                }
                else
                {
                    var sortScore = Last > 0 && (string.IsNullOrEmpty(OrderBy) || OrderBy.Is("score"));
                    WriteObject(ApplySkipAndTakeByPosition(query, First, Last, Skip, sortScore), true);
                }
                
            }
        }

    }
}