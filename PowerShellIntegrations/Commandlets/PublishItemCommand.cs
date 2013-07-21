using System;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Publish", "Item")]
    public class PublishItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public string[] Targets { get; set; }

        [Parameter]
        public string[] Languages { get; set; }

        [Parameter]
        public string PublishMode { get; set; }

        protected override void ProcessRecord()
        {
            Publish(Item, Path, Id, Recurse.IsPresent, Targets, Languages, PublishMode);
        }

        public void Publish(Item item, string path, string id, bool recursive, string[] targets,
                            string[] languages, string publishMode)
        {
            item = FindItemFromParameters(item, path, id);

            PublishMode mode;
            try
            {
                mode = (PublishMode) Enum.Parse(typeof (PublishMode), publishMode);
            }
            catch (Exception)
            {
                mode = Sitecore.Publishing.PublishMode.Smart;
            }

            if (item != null)
            {
                if (!item.Database.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
                {
                    throw new PSInvalidOperationException("Only items from the 'master' database can be published!");
                }

                if (targets != null)
                {
                    foreach (string target in targets)
                    {
                        PublishToTarget(item, target, recursive, languages, mode);
                    }
                }
                else
                {
                    PublishToTarget(item, "web", recursive, languages, mode);
                }
            }
        }

        private void PublishToTarget(Item item, string target, bool recursive, string[] languages,
                                     PublishMode mode)
        {
            if (languages == null)
            {
                Language language = Context.Language;
                PublishToTargetLanguage(item, target, language, recursive, mode);
            }
            else
            {
                foreach (Language siteLanguage in Context.Database.GetLanguages())
                {
                    foreach (string language in languages)
                    {
                        if (siteLanguage.CultureInfo.Name.Equals(language, StringComparison.OrdinalIgnoreCase))
                        {
                            PublishToTargetLanguage(item, target, siteLanguage, recursive, mode);
                        }
                    }
                }
            }
        }

        private void PublishToTargetLanguage(Item item, string target, Language language, bool recursive,
                                             PublishMode mode)
        {
            WriteVerbose(String.Format("Publishing item '{0}' in language '{1}' to target '{2}'", item.Name, language,
                                       target));
            WriteDebug(String.Format("[Debug]: Publishing item '{0}' in language '{1}' to target '{2}'", item.Name,
                                     language, target));
            Database webDb = Factory.GetDatabase(target);
            var options = new PublishOptions(Factory.GetDatabase("master"), webDb, mode, language, DateTime.Now)
                {
                    Deep = recursive
                };
            PublishItemPipeline.Run(item.ID, options);
        }
    }
}