using System.IO;
using System.Management.Automation;
using Sitecore.Install.Files;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Packages
{
    [Cmdlet("New", "ExplicitFileSource", DefaultParameterSetName = "File")]
    public class NewExplicitFileSourceCommand : BasePackageCommand
    {
        private ExplicitFileSource source;

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public FileSystemInfo File { get; set; }

        protected override void BeginProcessing()
        {
            source = new ExplicitFileSource {Name = Name};
        }

        protected override void ProcessRecord()
        {
            if (File is FileInfo)
            {
                source.Entries.Add((File as FileInfo).FullName);
            }
        }

        protected override void EndProcessing()
        {
            WriteObject(source, false);
        }
    }
}