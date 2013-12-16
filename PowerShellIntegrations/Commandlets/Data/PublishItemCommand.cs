using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet("Publish", "Item")]
    [OutputType(new Type[] {}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class PublishItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path")]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public string Id { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        [Alias("Targets")]
        public string[] Target { get; set; }

        [Parameter]
        [Alias("Languages")]
        public string[] Language { get; set; }

        [Parameter]
        public PublishMode PublishMode { get; set; }

        private List<WildcardPattern> languageWildcardPatterns { get; set; }

        protected override void BeginProcessing()
        {
            if (Language == null || !Language.Any())
            {
                languageWildcardPatterns = new List<WildcardPattern>();
            }
            else
            {
                languageWildcardPatterns =
                    Language.Select(
                        language =>
                            new WildcardPattern(language, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
            }
        }

        protected override void ProcessRecord()
        {
            Publish(Item, Path, Id);
        }

        private void Publish(Item item, string path, string id)
        {
            item = FindItemFromParameters(item, path, id);

            if (item != null)
            {
                if (!item.Database.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    throw new PSInvalidOperationException("Only items from the 'master' database can be published!");
                }

                if (Target != null)
                {
                    foreach (var target in Target)
                    {
                        PublishToTarget(item, target);
                    }
                }
                else
                {
                    PublishToTarget(item, "web");
                }
            }
        }

        private void PublishToTarget(Item item, string target)
        {
            if (languageWildcardPatterns.Count == 0)
            {
                if (Item != null)
                {
                    PublishToTargetLanguage(item, target, item.Language);
                }
                else
                {
                    var publishedLangs = new List<string>();
                    foreach (var langItem in item.Versions.GetVersions(true).Reverse())
                    {
                        if (!publishedLangs.Contains(langItem.Language.Name))
                        {
                            publishedLangs.Add(langItem.Language.Name);
                            PublishToTargetLanguage(langItem, target, langItem.Language);
                        }
                    }
                }
            }
            else
            {
                foreach (var siteLanguage in from siteLanguage in item.Database.GetLanguages()
                    from wildcard in languageWildcardPatterns
                    where wildcard.IsMatch(siteLanguage.Name)
                    select siteLanguage)
                {
                    PublishToTargetLanguage(item, target, siteLanguage);
                }
            }
        }

        private void PublishToTargetLanguage(Item item, string target, Language language)
        {
            if (PublishMode == PublishMode.Unknown)
            {
                PublishMode = PublishMode.Smart;
            }

            WriteVerbose(String.Format("Publishing item '{0}' in language '{1}', version '{2}' to target '{3}'",
                item.Name, language, item.Version, target));
            WriteDebug(String.Format("[Debug] Publishing item '{0}' in language '{1}', version '{2}' to target '{3}'",
                item.Name, language, item.Version, target));
            Database webDb = Factory.GetDatabase(target);
            var options = new PublishOptions(Factory.GetDatabase("master"), webDb, PublishMode, language, DateTime.Now)
            {
                Deep = Recurse.IsPresent
            };
            PublishItemPipeline.Run(item.ID, options);
        }
    }
}