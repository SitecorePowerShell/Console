using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.SitecoreIntegrations.Serialization;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;
using Sitecore.Globalization;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Serialization
{
    [Cmdlet("Serialize", "Item")]
    public class SerializeItemCommand : BaseCommand
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
        public string Target { get; set; }

        //[Parameter]
        public string[] Languages { get; set; }

        protected override void ProcessRecord()
        {
            if (Entry != null)
            {
                Serialize(Entry);
            }
            else
            {
                Serialize(Item, Path, Id, Recurse.IsPresent, Target, Languages, CurrentProviderLocation("CmsItemProvider"));
            }
        }

        public void Serialize(Item item, string path, string id, bool recursive, string target,
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
                SerializeToTarget(item, target, recursive, languages);
            }
            else
            {
                WriteError(
                    new ErrorRecord(
                        new InvalidOperationException("No item has been specified to the Serialize-Item cmdlet."),
                        "sitecore_no_item_provided", ErrorCategory.InvalidData, null));
            }
        }

        private void SerializeToTarget(Item item, string target, bool recursive, string[] languages)
        {
            if (!string.IsNullOrEmpty(target) && ItemPathsAbsolute.IsPresent)
            {
                target = target.EndsWith("\\")
                    ? target + item.Parent.Paths.FullPath.Replace("/", "\\")
                    : target + "\\" + item.Parent.Paths.FullPath.Replace("/", "\\");
            }

            if (languages == null)
            {
                Language language = item.Language;
                SerializeToTargetLanguage(item, target, language, recursive);
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
                        SerializeToTargetLanguage(item, target, siteLanguage, recursive);
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