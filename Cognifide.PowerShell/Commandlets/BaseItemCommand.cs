using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseItemCommand : BaseLanguageAgnosticItemCommand
    {
        public static string[] Cultures;

        static BaseItemCommand()
        {
            var cultures = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(culture => !string.IsNullOrEmpty(culture.Name)).Select(culture => culture.Name).OrderBy(p => p).ToList();
            cultures.Insert(0,"*");
            Cultures = cultures.ToArray();
        }

        [AutocompleteSet("Cultures")]
        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        [Parameter(ParameterSetName = "Item from Pipeline")]
        public virtual string[] Language { get; set; }

        protected List<WildcardPattern> LanguageWildcardPatterns { get; private set; }

        protected override void BeginProcessing()
        {
            if (Language == null || !Language.Any())
            {
                LanguageWildcardPatterns = new List<WildcardPattern>();
            }
            else
            {
                LanguageWildcardPatterns =
                    Language.Select(
                        language =>
                            new WildcardPattern(language, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
            }
            base.BeginProcessing();
        }

        protected override void ProcessRecord()
        {
            var sourceItem = FindItemFromParameters(Item, Path, Id, null, Database);

            if (sourceItem == null)
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find item to perform the operation on.", ErrorIds.ItemNotFound, ErrorCategory.InvalidData, null);
                return;
            }

            ProcessItemLanguages(sourceItem);
        }

        protected virtual void ProcessItemLanguages(Item item)
        {
            if (Language == null)
            {
                ProcessItem(item);
            }
            else
            {
                foreach (var langItem in LatestVersionInFilteredLanguages(item))
                {
                    ProcessItem(langItem);
                }
            }
        }

        protected List<Item> LatestVersionInFilteredLanguages(Item item)
        {
            var publishedLangs = new List<string>();
            var result = new List<Item>();
            // get all item versions in all languages
            foreach (var langItem in item.Versions.GetVersions(true).Reverse())
            {
                // publish latest version of each language
                if (LanguageWildcardPatterns.Any(wildcard => !publishedLangs.Contains(langItem.Language.Name) &&
                                                             wildcard.IsMatch(langItem.Language.Name)))
                {
                    publishedLangs.Add(langItem.Language.Name);
                    result.Add(langItem);
                }
            }
            return result;
        }
    }
}