using System.Management.Automation;
using Cognifide.PowerShell.SitecoreIntegrations.Serialization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Serialization
{
    [Cmdlet("Deserialize", "Item")]
    public class DeserializeItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Database Database { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public IncludeEntry Entry { get; set; }

        [Parameter]
        [Alias("FullName")]
        public string Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public string Root { get; set; }

        [Parameter]
        public SwitchParameter UseNewId { get; set; }

        [Parameter]
        public SwitchParameter DisableEvents { get; set; }

        [Parameter]
        public SwitchParameter ForceUpdate { get; set; }

        protected override void ProcessRecord()
        {
            if (Database != null)
            {
                Deserialize(Database);
            }
            else if (Item != null)
            {
                Deserialize(Item);
            }
            else if (Entry != null)
            {
                Deserialize(Entry);
            }
            else if (Path != null)
            {
                Deserialize(Path);
            }
        }

        public void Deserialize(Database database)
        {
            LoadOptions options = GetLoadOptions();
            string path = System.IO.Path.Combine(options.Root, database.Name);
            Manager.LoadTree(path, options);
        }

        public void Deserialize(Item item)
        {
            ItemReference reference = new ItemReference(item);
            if (Recurse.IsPresent)
            {
                Manager.LoadTree(PathUtils.GetDirectoryPath(reference.ToString()), GetLoadOptions());
            }
            else
            {
                Manager.LoadItem(PathUtils.GetFilePath(reference.ToString()), GetLoadOptions());
            }
        }

        public void Deserialize(string path)
        {
            if (Recurse.IsPresent)
            {
                Manager.LoadTree(path, GetLoadOptions());
            }
            else
            {
                Manager.LoadItem(path, GetLoadOptions());
            }
        }

        public void Deserialize(IncludeEntry entry)
        {
            PresetWorker worker = new PresetWorker(entry);
            worker.Deserialize(GetLoadOptions());
        }

        private LoadOptions GetLoadOptions()
        {
            var options = new LoadOptions
            {
                UseNewID = UseNewId.IsPresent,
                DisableEvents = DisableEvents.IsPresent,
                ForceUpdate = ForceUpdate.IsPresent,
            };

            if (Root != null)
            {
                options.Root = Root;
            }

            return options;
        }
    }
}