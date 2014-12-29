using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;

namespace Cognifide.PowerShell.Commandlets.Data
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
            if (item.Database.Name.IsNot("master"))
            {
                var error = String.Format("Item '{0}' cannot be published. Only items from the 'master' database can be published!", item.Name);
                WriteError(new ErrorRecord(new PSInvalidOperationException(error), error, ErrorCategory.InvalidData, null));
                return;
            }

            var source = Factory.GetDatabase("master");

            if (Target != null)
            {
                var targets = Target.Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();
                foreach (var target in targets.Select(Factory.GetDatabase))
                {
                    PublishToTarget(item, source, target);
                }
            }
            else
            {
                PublishToTarget(item, source, Factory.GetDatabase("web"));
            }
        }

        private void PublishToTarget(Item item, Database source, Database target)
        {
            if (PublishMode == PublishMode.Unknown)
            {
                PublishMode = PublishMode.Smart;
            }
            
            var language = item.Language;

            WriteVerbose(String.Format("Publishing item '{0}' in language '{1}', version '{2}' to target '{3}'.  (Recurse={4}).",
                item.Name, language, item.Version, target.Name, Recurse.IsPresent));

            var options = new PublishOptions(source, target, PublishMode, language, DateTime.Now)
            {
                Deep = Recurse.IsPresent,
                RootItem = item
            };

            PublishOptions[] optionsArgs = new PublishOptions[1];
            optionsArgs[0] = options;

            var handle = PublishManager.Publish(optionsArgs);

            if (handle != null)
            {
                var publishStatus = PublishManager.GetStatus(handle) ?? new PublishStatus();

                WriteVerbose(String.Format("Publish Job submitted, current state={0}.",publishStatus.State.ToString()));
            }
        }
    }
}