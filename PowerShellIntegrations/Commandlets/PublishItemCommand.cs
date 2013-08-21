using System.Collections.Generic;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using System;
using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Publish", "Item")]
    public class PublishItemCommand : BaseCommand
    {
        private IEnumerable<Language> siteLanguages;

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter]
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
                siteLanguages = Context.Database.GetLanguages();
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
                    foreach (string target in Target)
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
                foreach (Language language in item.Languages)
                {
                    PublishToTargetLanguage(item, target, language);
                }
            }
            else
            {
                foreach (Language siteLanguage in from siteLanguage in siteLanguages
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

            WriteVerbose(String.Format("Publishing item '{0}' in language '{1}' to target '{2}'", item.Name, language,
                                       target));
            WriteDebug(String.Format("[Debug]: Publishing item '{0}' in language '{1}' to target '{2}'", item.Name,
                                     language, target));
            Database webDb = Factory.GetDatabase(target);
            var options = new PublishOptions(Factory.GetDatabase("master"), webDb, PublishMode, language, DateTime.Now)
                {
                    Deep = Recurse.IsPresent
                };
            PublishItemPipeline.Run(item.ID, options);
        }
    }
}