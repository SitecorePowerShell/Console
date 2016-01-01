using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;

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
        [AutocompleteSet("Databases")]
        public string[] Target { get; set; }

        [Parameter]
        public PublishMode PublishMode { get; set; }
        
        [Parameter]
        public SwitchParameter PublishRelatedItems { get; set; }

        [Parameter]
        public SwitchParameter RepublishAll { get; set; }

        [Parameter]
        public SwitchParameter CompareRevisions { get; set; }

        [Parameter]
        public DateTime FromDate { get; set; }

        [Parameter]
        public SwitchParameter Synchronous { get; set; }

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
                foreach (var publishingTarget in PublishManager.GetPublishingTargets(source))
                {
                    var destination = Factory.GetDatabase(publishingTarget[FieldIDs.PublishingTargetDatabase]);
                    PublishToTarget(item, source, destination);
                }
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
                WriteVerbose($"Publishing item '{item.Name}' in language '{language}', version '{item.Version}' to target '{target.Name}'.  (Recurse={Recurse.IsPresent}).");

                var options = new PublishOptions(source, target, PublishMode, language, DateTime.Now)
                {
                    Deep = Recurse.IsPresent,
                    RootItem = item
                };

                var optionsArgs = new PublishOptions[1];
                optionsArgs[0] = options;

                // new
                options.RepublishAll = RepublishAll;
                options.CompareRevisions = CompareRevisions;
                options.FromDate = FromDate;
                if (this.VersionSupportThreshold(nameof(PublishRelatedItems), VersionResolver.SitecoreVersion72, true))
                {
                    PublishRelatedItems72(options);
                }

                if (!Synchronous)
                {
                    var handle = PublishManager.Publish(optionsArgs);
                    if (handle == null) return;
                    var publishStatus = PublishManager.GetStatus(handle) ?? new PublishStatus();
                    WriteVerbose($"Publish Job submitted, current state={publishStatus.State}.");
                }
                else
                {
                    var publishContext = PublishManager.CreatePublishContext(options);
                    var stats = PublishPipeline.Run(publishContext)?.Statistics;
                    WriteVerbose("Publish Finished.");
                    if (stats != null)
                    {
                        WriteVerbose($"Items Created={stats.Created}, Deleted={stats.Deleted}, Skipped={stats.Skipped}, Updated={stats.Updated}.");
                    }
                }
            }
        }

        private void PublishRelatedItems72(PublishOptions options)
        {
            options.PublishRelatedItems = PublishRelatedItems;
        }
    }
}