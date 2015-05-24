using System;
using System.IO;
using System.Management.Automation;
using Sitecore.Install.Configuration;
using Sitecore.Install.Files;
using Sitecore.Install.Utils;
using Sitecore.IO;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "ExplicitFileSource")]
    [OutputType(typeof(ExplicitFileSource))]
    public class NewExplicitFileSourceCommand : BasePackageCommand
    {
        private ExplicitFileSource source;

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public FileSystemInfo File { get; set; }

        [Parameter]
        [ValidateSet("Undefined", "Overwrite", "Skip")]
        public string InstallMode { get; set; }

        protected override void BeginProcessing()
        {
            source = new ExplicitFileSource { Name = Name };
            
            var mode = (InstallMode)Enum.Parse(typeof(InstallMode), InstallMode);
            if (mode != Sitecore.Install.Utils.InstallMode.Undefined)
            {
                source.Converter.Transforms.Add(
                    new InstallerConfigurationTransform(
                        new BehaviourOptions(mode, MergeMode.Undefined)));
            }
        }

        protected override void ProcessRecord()
        {
            if (!(File is FileInfo)) return;

            var siteRoot = FileUtil.MapPath("/");
            var fullName = ((FileInfo) File).FullName;
            if (fullName.StartsWith(siteRoot, StringComparison.OrdinalIgnoreCase))
            {
                fullName = fullName.Substring(siteRoot.Length - 1).Replace('\\', '/');
            }
            source.Entries.Add(fullName);
        }

        protected override void EndProcessing()
        {
            WriteObject(source, false);
        }
    }
}