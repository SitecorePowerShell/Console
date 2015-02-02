using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Serialization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Export, "Item")]
    public class ExportItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public IncludeEntry Entry { get; set; }

        [Parameter]
        [Alias("FullName", "FileName")]
        public string Path { get; set; }

        [Parameter]
        public string Id { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public SwitchParameter ItemPathsAbsolute { get; set; }

        [Parameter]
        [Alias("Target")]
        public string Root { get; set; }

        //[Parameter]
        [Alias("Languages")]
        public string[] Language { get; set; }

        protected override void ProcessRecord()
        {
            if (Entry != null)
            {
                Serialize(Entry);
            }
            else
            {
                Serialize(Item, Path, Id, Recurse.IsPresent, Root, Language, CurrentProviderLocation("CmsItemProvider"));
            }
        }

        public void Serialize(Item item, string path, string id, bool recursive, string root,
            string[] languages, PathInfo currentPathInfo)
        {
            if (item == null)
            {
                if (!String.IsNullOrEmpty(id))
                {
                    Database currentDb = Factory.GetDatabase(currentPathInfo.Drive.Name);
                    item = currentDb.GetItem(new ID(id));
                }
                else if (!String.IsNullOrEmpty(path))
                {
                    path = path.Replace('\\', '/');
                    item = PathUtilities.GetItem(path, currentPathInfo.Drive.Name, currentPathInfo.ProviderPath);
                }
            }

            if (item != null)
            {
                SerializeToTarget(item, root, recursive, languages);
            }
            else
            {
                WriteError(
                    new ErrorRecord(
                        new InvalidOperationException("No item has been specified to the Serialize-Item cmdlet."),
                        "sitecore_no_item_provided", ErrorCategory.InvalidData, null));
            }
        }

        private void SerializeToTarget(Item item, string root, bool recursive, string[] languages)
        {
            if (!string.IsNullOrEmpty(root) && ItemPathsAbsolute.IsPresent)
            {
                root = root.EndsWith("\\")
                    ? root + item.Parent.Paths.FullPath.Replace("/", "\\")
                    : root + "\\" + item.Parent.Paths.FullPath.Replace("/", "\\");
            }

            if (languages == null)
            {
                Language language = item.Language;
                SerializeToTargetLanguage(item, root, language, recursive);
            }
            else
            {
                foreach (var siteLanguage in item.Database.GetLanguages())
                {
                    if (
                        languages.Any(
                            language =>
                                siteLanguage.CultureInfo.Name.Equals(language, StringComparison.OrdinalIgnoreCase)))
                    {
                        SerializeToTargetLanguage(item, root, siteLanguage, recursive);
                    }
                }
            }
        }

        private void SerializeToTargetLanguage(Item item, string target, Language language, bool recursive)
        {
            WriteVerbose(String.Format("Serializing item '{0}' to target '{1}'", item.Name, target));
            WriteDebug(String.Format("[Debug]: Serializing item '{0}' to target '{1}'", item.Name, target));

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
                    SerializeToTargetLanguage(child, target, language, true);
                }
            }
        }

        public void Serialize(IncludeEntry entry)
        {
            PresetWorker worker = new PresetWorker(entry);
            worker.Serialize();
        }
    }
}