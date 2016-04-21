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
    [OutputType(new Type[] { }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class PublishItemCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        [Alias("Targets")]
        [AutocompleteSet("Databases")]
        public string[] Target { get; set; }

        [Parameter]
        public PublishMode PublishMode { get; set; } = PublishMode.Smart;

        [Parameter]
        public SwitchParameter PublishRelatedItems { get; set; }

        [Parameter]
        public SwitchParameter RepublishAll { get; set; }

        [Parameter]
        public SwitchParameter CompareRevisions { get; set; }

        [Parameter]
        public DateTime FromDate { get; set; }

        [Parameter]
        public SwitchParameter AsJob { get; set; }

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
                    var target = Factory.GetDatabase(publishingTarget[FieldIDs.PublishingTargetDatabase]);
                    PublishToTarget(item, source, target);
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
                    Deep = Recurse,
                    RootItem = (PublishMode == PublishMode.Incremental) ? null : item,
                    RepublishAll = RepublishAll,
                    CompareRevisions = CompareRevisions || PublishMode == PublishMode.Smart
                };

                if (PublishMode == PublishMode.Incremental)
                {
                    WriteVerbose("Incremental publish causes ALL Database Items that are in the publishing queue to be published.");
                }

                if (!CompareRevisions && IsParameterSpecified(nameof(CompareRevisions)) && (PublishMode == PublishMode.Smart))
                {
                    WriteWarning($"The -{nameof(CompareRevisions)} parameter is set to $false but required to be $true when -{nameof(PublishMode)} is set to {PublishMode.Smart}, forcing {CompareRevisions} to $true.");
                }

                if (IsParameterSpecified(nameof(FromDate)))
                {
                    options.FromDate = FromDate;
                }

                if (PublishRelatedItems)
                {
                    SitecoreVersion.V72
                        .OrNewer(() =>
                        {
                            options.PublishRelatedItems = PublishRelatedItems;
                            // Below blog explains why we're forcing Single Item 
                            // http://www.sitecore.net/learn/blogs/technical-blogs/reinnovations/posts/2014/03/related-item-publishing.aspx
                            if (PublishRelatedItems && IsParameterSpecified(nameof(PublishMode)) && (PublishMode != PublishMode.SingleItem))
                            {
                                WriteWarning($"The -{nameof(PublishRelatedItems)} parameter is used which requires -{nameof(PublishMode)} to be set to set to {PublishMode.SingleItem}, forcing {PublishMode.SingleItem} PublishMode.");
                            }
                            options.Mode = PublishMode.SingleItem;
                        }).ElseWriteWarning(this, nameof(PublishRelatedItems), true);
                }

                if (AsJob)
                {
                    var publisher = new Publisher(options);
                    var job = publisher.PublishAsync();

                    if (job == null) return;
                    WriteObject(job);
                }
                else
                {
                    var publishContext = PublishManager.CreatePublishContext(options);
                    SitecoreVersion.V72.OrNewer(() => 
                        {
                            publishContext.Languages = new[] { language };
                            var stats = PublishPipeline.Run(publishContext)?.Statistics;
                            if (stats != null)
                            {
                                WriteVerbose($"Items Created={stats.Created}, Deleted={stats.Deleted}, Skipped={stats.Skipped}, Updated={stats.Updated}.");
                            }
                        }).Else(
                            () =>
                            {
                                PublishPipeline.Run(publishContext);
                            });
                    WriteVerbose("Publish Finished.");
                }
            }
        }
    }
}