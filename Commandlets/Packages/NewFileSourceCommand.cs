using System.Management.Automation;
using Sitecore.Install.Files;
using Sitecore.Install.Filters;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "FileSource")]
    [OutputType(typeof (FileSource))]
    public class NewFileSourceCommand : BasePackageCommand
    {
        private FileSource source;

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(Position = 1)]
        public string Root { get; set; }

        [Parameter(Position = 2)]
        public string IncludeFilter { get; set; }

        [Parameter(Position = 3)]
        public string ExcludeFilter { get; set; }

        protected override void ProcessRecord()
        {
            source = new FileSource {Name = Name, Root = Root, Converter = new FileToEntryConverter()};

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