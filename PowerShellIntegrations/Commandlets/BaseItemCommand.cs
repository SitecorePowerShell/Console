using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    public abstract class BaseItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public string Id { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public Database Database { get; set; }

        [Alias("Languages")]
        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        public string[] Language { get; set; }

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
        }

        protected override void ProcessRecord()
        {
            Item sourceItem = FindItemFromParameters(Item, Path, Id, null, Database);

            if (sourceItem == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException(
                            "Item not found."),
                        "sitecore_item_not_found", ErrorCategory.InvalidData, null));
                return;
            }

            ProcessItemLanguages(sourceItem);
        }

        private void ProcessItemLanguages(Item item)
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

        protected abstract void ProcessItem(Item item);

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