using System;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Install.Configuration;
using Sitecore.Install.Files;
using Sitecore.Install.Filters;
using Sitecore.Install.Utils;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "FileSource")]
    [OutputType(typeof(FileSource))]
    public class NewFileSourceCommand : BasePackageCommand
    {
        private FileSource source;

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1, Mandatory = true, ValueFromPipeline = true)]
        public DirectoryInfo Root { get; set; }

        [Parameter(Position = 2)]
        public string IncludeFilter { get; set; }

        [Parameter(Position = 3)]
        public string ExcludeFilter { get; set; }

        [Parameter]
        [ValidateSet("Undefined", "Overwrite", "Skip")]
        public string InstallMode { get; set; }

        protected override void ProcessRecord()
        {
            source = new FileSource { Name = Name, Root = PathUtilities.GetRelativePath(Root.FullName), Converter = new FileToEntryConverter()};

            if (!String.IsNullOrEmpty(InstallMode))
            {
                var mode = (InstallMode) Enum.Parse(typeof (InstallMode), InstallMode);
                if (mode != Sitecore.Install.Utils.InstallMode.Undefined)
                {
                    source.Converter.Transforms.Add(
                        new InstallerConfigurationTransform(
                            new BehaviourOptions(mode, MergeMode.Undefined)));
                }
            }

            if (string.IsNullOrEmpty(IncludeFilter))
            {
                IncludeFilter = "*.*";
            }

            source.Include.Add(new FileNameFilter(IncludeFilter));
            source.Include.Add(new FileDateFilter(FileDateFilter.FileDateFilterType.CreatedFilter));
            source.Include.Add(new FileDateFilter(FileDateFilter.FileDateFilterType.ModifiedFilter));

            if (!string.IsNullOrEmpty(ExcludeFilter))
            {
                var filter = new FileNameFilter(ExcludeFilter);
                source.Exclude.Add(filter);
            }
            WriteObject(source, false);
        }
    }
}