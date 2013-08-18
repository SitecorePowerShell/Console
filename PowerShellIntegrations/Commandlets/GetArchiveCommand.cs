using System;
using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Archiving;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Get", "Archive", DefaultParameterSetName = "Name")]
    [OutputType(new[] { typeof(Archive) })]
    public class GetArchiveCommand : DatabaseContextBaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 0)]
        public override string Name { get; set; }

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            if (String.IsNullOrEmpty(Name))
            {
                foreach (Database database in databases)
                {
                    WriteObject(ArchiveManager.GetArchives(database), true);
                }
            }
            else
            {
                foreach (Database database in databases)
                {
                    WildcardWrite(Name, ArchiveManager.GetArchives(database), archive => archive.Name);
                }
            }
        }
    }
}