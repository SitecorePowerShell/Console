using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsData.Publish, "Item", SupportsShouldProcess = true)]
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
                WriteError(typeof(PSInvalidOperationException), $"Item '{item.Name}' cannot be published. Only items from the 'master' database can be published!", 
                    ErrorIds.InvalidOperation, ErrorCategory.InvalidOperation, null);
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

            if (ShouldProcess(item.GetProviderPath(),
                string.Format("{3}ublishing language '{0}', version '{1}' to target '{2}'.", language, item.Version,
                    target.Name, Recurse.IsPresent ? "Recursively p" : "P")))
            {
                WriteVerbose(
                    String.Format(
                        "Publishing item '{0}' in language '{1}', version '{2}' to target '{3}'.  (Recurse={4}).",
                        item.Name, language, item.Version, target.Name, Recurse.IsPresent));

                var options = new PublishOptions(source, target, PublishMode, language, DateTime.Now)
                {
                    Deep = Recurse.IsPresent,
                    RootItem = item
                };

                var optionsArgs = new PublishOptions[1];
                optionsArgs[0] = options;

                var handle = PublishManager.Publish(optionsArgs);

                if (handle != null)
                {
                    var publishStatus = PublishManager.GetStatus(handle) ?? new PublishStatus();

                    WriteVerbose(String.Format("Publish Job submitted, current state={0}.",
                        publishStatus.State));
                }
            }
        }
    }
}