using System.Management.Automation;
using Cognifide.PowerShell.Core.Serialization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;
using Sitecore.Data.Serialization.Presets;

namespace Cognifide.PowerShell.Commandlets.Serialization
{
    [Cmdlet(VerbsData.Import, "Item", SupportsShouldProcess = true)]
    [OutputType(typeof (void), ParameterSetName = new[] {"Database", "Item", "Preset", "Path"})]
    public class ImportItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Database")]
        public Database Database { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Item", Mandatory = true, Position = 0)]
        public Item Item { get; set; }

        [Alias("Entry")]
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Preset", Mandatory = true, Position = 0)]
        public IncludeEntry Preset { get; set; }

        [Parameter(ParameterSetName = "Path", Mandatory = true, Position = 0)]
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
            var options = GetLoadOptions();
            var path = System.IO.Path.Combine(options.Root, database.Name);
            options.Database = database;
            if (ShouldProcess(path, "Deserializing database"))
            {
                Manager.LoadTree(path, options);
            }
        }

        public void Deserialize(Item item)
        {
            var reference = new ItemReference(item);
            if (Recurse.IsPresent)
            {
                var path = PathUtils.GetDirectoryPath(reference.ToString());
                Deserialize(path);
            }
            else
            {
                var path = PathUtils.GetFilePath(reference.ToString());
                Deserialize(path);
            }
        }

        public void Deserialize(string path)
        {
            if (Recurse.IsPresent)
            {
                if (ShouldProcess(path, "Deserializing tree"))
                {
                    Manager.LoadTree(path, GetLoadOptions());
                }
            }
            else
            {
                if (ShouldProcess(path, "Deserializing item"))
                {
                    Manager.LoadItem(path, GetLoadOptions());
                }
            }
        }

        public void Deserialize(IncludeEntry entry)
        {
            var worker = new PresetWorker(entry);
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

            options.Root = Root ?? PathUtils.Root;

            return options;
        }
    }
}