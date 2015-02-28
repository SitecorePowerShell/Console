using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Serialization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Collections;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Export, "Item", SupportsShouldProcess = true)]
    public class ExportItemCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Items from Preset")]
        public IncludeEntry Entry { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public SwitchParameter ItemPathsAbsolute { get; set; }

        [Parameter]
        [Alias("Target")]
        public string Root { get; set; }

        protected override void ProcessRecord()
        {
            if (Entry != null)
            {
                Serialize(Entry);
            }
            else
            {
                base.ProcessRecord();
            }
        }

        protected override void ProcessItem(Item item)
        {
            if (item != null)
            {
                SerializeToTarget(item, Root, Recurse);
            }
            else
            {
                WriteError(
                    new ErrorRecord(
                        new InvalidOperationException("No item has been specified to the Serialize-Item cmdlet."),
                        "sitecore_no_item_provided", ErrorCategory.InvalidData, null));
            }
        }

        private void SerializeToTarget(Item item, string target, bool recursive)
        {
            if (!string.IsNullOrEmpty(target) && ItemPathsAbsolute.IsPresent)
            {
                target = target.EndsWith("\\")
                    ? target + item.Parent.Paths.FullPath.Replace("/", "\\")
                    : target + "\\" + item.Parent.Paths.FullPath.Replace("/", "\\");
            }

            var message = string.Format("Serializing item '{0}' to target '{1}'", item.Name, target);
            WriteVerbose(message);
            WriteDebug(message);

            var fileName = target;
            if (string.IsNullOrEmpty(fileName))
            {
                var itemReference = new ItemReference(item);
                fileName = PathUtils.GetFilePath(itemReference.ToString());
            }
            if (!ShouldProcess(item.GetProviderPath(), string.Format("Serializing item to '{0}'", fileName)))
            {
                return;
            }

            if (string.IsNullOrEmpty(target))
            {
                Manager.DumpItem(item);
            }
            else
            {
                target = target.EndsWith("\\") ? target + item.Name : target + "\\" + item.Name;
                Manager.DumpItem(target + ".item", item);
            }
            if (recursive)
            {
                foreach (Item child in item.GetChildren(ChildListOptions.IgnoreSecurity))
                {
                    SerializeToTarget(child, target, true);
                }
            }
        }

        public void Serialize(IncludeEntry entry)
        {
            var worker = new PresetWorker(entry);
            worker.Serialize();
        }
    }
}