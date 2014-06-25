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
    [Cmdlet(VerbsData.Publish, "Item")]
    [OutputType(new Type[] {}, ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class PublishItemCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        [Alias("Targets")]
        public string[] Target { get; set; }

        [Parameter]
        public PublishMode PublishMode { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (!item.Database.Name.Equals("master", StringComparison.OrdinalIgnoreCase))
            {
                WriteError(
                    new ErrorRecord(
                        new PSInvalidOperationException("Only items from the 'master' database can be published!"),
                        "sitecore_publishing_source_is_not_master_db", ErrorCategory.InvalidData, null));
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

        private void PublishToTarget(Item item, string target)
        {
            PublishToTargetLanguage(item, target, item.Language);
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