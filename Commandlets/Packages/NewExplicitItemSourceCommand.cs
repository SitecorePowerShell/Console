using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Install.Configuration;
using Sitecore.Install.Items;
using Sitecore.Install.Utils;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "ExplicitItemSource")]
    [OutputType(typeof(ExplicitItemSource))]
    public class NewExplicitItemSourceCommand : BasePackageCommand
    {
        private ExplicitItemSource source;

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        public Item Item { get; set; }

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public SwitchParameter SkipVersions { get; set; }

        [Parameter]
        public InstallMode InstallMode { get; set; }

        [Parameter]
        public MergeMode MergeMode { get; set; }

        protected override void BeginProcessing()
        {
            source = new ExplicitItemSource { Name = Name, SkipVersions = SkipVersions.IsPresent };
            
            if (InstallMode != InstallMode.Undefined)
            {
                source.Converter.Transforms.Add(
                    new InstallerConfigurationTransform(new BehaviourOptions(InstallMode, MergeMode)));
            }
        }

        protected override void ProcessRecord()
        {
            source.Entries.Add(
                new ItemReference(Item.Database.Name, Item.Paths.Path, Item.ID, Language.Invariant, Version.Latest)
                    .ToString());
        }

        protected override void EndProcessing()
        {
            WriteObject(source, false);
        }
    }
}