using System.Management.Automation;
using Cognifide.PowerShell.Core.Serialization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Import, "Item")]
    [OutputType(new[] { typeof(void)}, ParameterSetName = new[] { "Database", "Item", "Preset", "Path" })]
    public class ImportItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Database")]
        public Database Database { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Item")]
        public Item Item { get; set; }

        [Alias("Entry")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Preset")]
        public IncludeEntry Preset { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Alias("FullName")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "Item")]
        public SwitchParameter Recurse { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "Preset")]
        [Parameter(ParameterSetName = "Item")]
        [Parameter(ParameterSetName = "Database")]
        public string Root { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "Item")]
        [Parameter(ParameterSetName = "Database")]
        [Parameter(ParameterSetName = "Preset")]
        public SwitchParameter UseNewId { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "Item")]
        [Parameter(ParameterSetName = "Database")]
        [Parameter(ParameterSetName = "Preset")]
        public SwitchParameter DisableEvents { get; set; }

        [Parameter(ParameterSetName = "Path")]
        [Parameter(ParameterSetName = "Item")]
        [Parameter(ParameterSetName = "Database")]
        [Parameter(ParameterSetName = "Preset")]
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
            else if (Preset != null)
            {
                Deserialize(Preset);
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
                UseNewID = UseNewId,
                DisableEvents = DisableEvents,
                ForceUpdate = ForceUpdate
            };

            if (Root != null)
            {
                options.Root = Root;
            }

            return options;
        }
    }
}