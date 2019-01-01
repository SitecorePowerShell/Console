using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Archiving;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Remove, "ItemVersion", SupportsShouldProcess = true)]
    public class RemoveItemVersionCommand : BaseItemCommand
    {
        private string confirmMessage;

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Alias("Languages")]
        [AutocompleteSet(nameof(Cultures))]
        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline")]
        public override string[] Language { get; set; }

        [Alias("Versions")]
        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        [Parameter(ParameterSetName = "Item from Pipeline")]
        public string[] Version { get; set; } = new string[0];

        [Alias("ExcludeLanguages", "ExcludedLanguages", "ExcludedLanguage")]
        [Parameter(ParameterSetName = "Item from Path")]
        [Parameter(ParameterSetName = "Item from ID")]
        [Parameter(ParameterSetName = "Item from Pipeline")]
        public virtual string[] ExcludeLanguage { get; set; }

        protected List<WildcardPattern> ExcludeLanguageWildcardPatterns { get; private set; }
        protected List<WildcardPattern> VersionPatterns { get; private set; }
        protected List<string> ProcessedList { get; private set; }

        [Alias("MaxVersions")]
        [Parameter]
        public int MaxRecentVersions { get; set; } = 0;

        [Parameter]
        public SwitchParameter Permanently { get; set; }

        [Parameter]
        public SwitchParameter Archive { get; set; }

        protected override void BeginProcessing()
        {
            if (ExcludeLanguage == null || !ExcludeLanguage.Any())
            {
                ExcludeLanguageWildcardPatterns = new List<WildcardPattern>();
            }
            else
            {
                ExcludeLanguageWildcardPatterns =
                    ExcludeLanguage.Select(
                        language =>
                            new WildcardPattern(language, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();

                if (Language == null || !Language.Any())
                {
                    Language = new[] { "*" };
                }
            }

            var langsList = Language?.Aggregate((seed, curr) => seed + ", " + curr) ?? "not specified";
            var excludedLangsMessage = (ExcludeLanguage == null || ExcludeLanguage.Length == 0)
                ? ""
                : $"excluding {ExcludeLanguage.Aggregate((seed, curr) => seed + ", " + curr)} language(s)";
            confirmMessage =
                $"R{(Recurse ? "ecursively r" : "")}emove versions for language(s) '{langsList}' {excludedLangsMessage}";

            if (Version != null && Version.Any())
            {
                VersionPatterns =
                    Version.Select(
                        version => new WildcardPattern(version, WildcardOptions.IgnoreCase | WildcardOptions.Compiled))
                        .ToList();
            }
            else
            {
                VersionPatterns = new List<WildcardPattern>();
            }

            base.BeginProcessing();
        }

        protected override void ProcessItemLanguages(Item item)
        {
            ProcessedList = new List<string>();

            if (IsParameterSpecified(nameof(Language)) || IsParameterSpecified(nameof(ExcludeLanguage)))
            {
                foreach (
                    var langItem in
                        item.Versions.GetVersions(true)
                            .Where(
                                langItem =>
                                    LanguageWildcardPatterns.Any(wildcard => wildcard.IsMatch(langItem.Language.Name)))
                            .Where(
                                langItem =>
                                    !ExcludeLanguageWildcardPatterns.Any(
                                        wildcard => wildcard.IsMatch(langItem.Language.Name))))
                {
                    TrimVersions(langItem);
                }
            }
            else
            {
                TrimVersions(item);
            }
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    ProcessItemLanguages(child);
                }
            }
        }

        private void TrimVersions(Item langItem)
        {
            // no specific version number provided
            if (!Version.Any())
            {
                // version number trimming
                if (IsParameterSpecified(nameof(MaxRecentVersions)))
                {
                    if (langItem.Versions.Count > MaxRecentVersions)
                    {
                        langItem.Versions.GetVersionNumbers()
                            .Take(langItem.Versions.Count - MaxRecentVersions)
                            .ForEach(verNo => RemoveVersion(langItem.Versions[verNo]));
                    }
                }
                else
                {
                    //just remove the piped version
                    RemoveVersion(langItem);
                }
            }
            else
            {
                // versions specified - check if matching filter
                if (VersionPatterns.Any(wildcard => wildcard.IsMatch(langItem.Version.Number.ToString())))
                {
                    RemoveVersion(langItem);
                }
            }
        }

        private void RemoveVersion(Item item)
        {
            var itemSig = $"{item.Database}:{item.ID}/{item.Language}#{item.Version}";
            if (ProcessedList.Contains(itemSig)) return;

            ProcessedList.Add(itemSig);
            if (!ShouldProcess(item.GetProviderPath() + ", Lang:" + item.Language.Name + ", Ver:" + item.Version.Number, confirmMessage)) { return; }

            var hasArchive = IsParameterSpecified(nameof(Archive));
            var hasPermanently = IsParameterSpecified(nameof(Permanently));

            if (hasArchive && hasPermanently)
            {
                WriteError(typeof(ParameterBindingException), "Parameter set cannot be resolved using the specified named parameters. Detected Archive and Permanently parameters provided.", ErrorIds.AmbiguousParameterSet, ErrorCategory.InvalidOperation, null);
                return;
            }

            if (IsParameterSpecified("Archive"))
            {
                var archive = ArchiveManager.GetArchive("archive", item.Database);
                if (archive == null) return;
                WriteVerbose($"Removing item {itemSig} and moving to the archive {archive.Name} in database {item.Database}");
                archive.ArchiveVersion(item);
            }
            else if (IsParameterSpecified("Permanently"))
            {
                WriteVerbose($"Removing item {itemSig} permanently.");
                item.Versions.RemoveVersion();
            }
            else
            {
                var archive = ArchiveManager.GetArchive("recyclebin", item.Database);
                if (archive == null) return;
                WriteVerbose($"Removing item {itemSig} and moving to the archive {archive.Name} in database {item.Database}");
                archive.ArchiveVersion(item);
            }
        }

        protected override void ProcessItem(Item item)
        {
            // this function is not used due to override on ProcessItemLanguages
        }
    }
}