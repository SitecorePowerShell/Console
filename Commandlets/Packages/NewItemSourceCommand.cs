using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Install.Configuration;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "ItemSource")]
    [OutputType(typeof (ItemSource))]
    public class NewItemSourceCommand : BasePackageCommand
    {
        private ItemSource source;

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter SkipVersions { get; set; }

        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        public string Database { get; set; }

        [Parameter(Position = 3)]
        public string Root { get; set; }

        [Parameter]
        public InstallMode InstallMode { get; set; }

        [Parameter]
        public MergeMode MergeMode { get; set; }

        protected override void ProcessRecord()
        {
            source = new ItemSource {Name = Name}; //Create source – source should be based on BaseSource              
            if (Item != null)
            {
                source.Database = Item.Database.Name;
                source.Root = Item.Paths.Path;
            }
            else
            {
                source.Database = Database;
                source.Root = Root;
            }
            source.SkipVersions = SkipVersions.IsPresent;

            if (InstallMode != InstallMode.Undefined)
            {
                source.Converter.Transforms.Add(
                    new InstallerConfigurationTransform(new BehaviourOptions(InstallMode, MergeMode)));
            }
        }

        protected override void EndProcessing()
        {
            WriteObject(source, false);
        }
    }
}