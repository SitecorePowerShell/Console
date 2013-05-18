using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Serialization;

namespace Cognifide.PowerShell.Shell.Commands.Serialization
{
    [Cmdlet("Deserialize", "Item", SupportsShouldProcess = true, DefaultParameterSetName = "Item")]
    public class DeserializeItemCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Database Database { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter]
        public string Path { get; set; }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public string Root { get; set; }

        [Parameter]
        public SwitchParameter UseNewId { get; set; }

        [Parameter]
        protected SwitchParameter DisableEvents { get; set; }

        protected override void ProcessRecord()
        {
            Deserialize(Path);
        }

        public void Deserialize(string path)
        {
            var options = new LoadOptions
                {
                    Database = Database,
                    Root = Root,
                    UseNewID = UseNewId.IsPresent,
                    DisableEvents = DisableEvents.IsPresent
                };

            if (Recurse.IsPresent)
            {
                Manager.LoadTree(path, options);
            }
            else
            {
                Manager.LoadItem(path, options);
            }
        }
    }
}