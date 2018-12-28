using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Archiving;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "Archive")]
    [OutputType(typeof (Archive))]
    public class GetArchiveCommand : DatabaseContextBaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 0)]
        public override string Name { get; set; }

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            foreach (var database in databases)
            {
                var archives = ArchiveManager.GetArchives(database);
                var extendedArchives = new List<PSObject>();
                foreach (var archive in archives)
                {
                    var extendedArchive = new PSObject(archive);
                    extendedArchive.Properties.Add(new PSNoteProperty("Database", database));
                    extendedArchive.Properties.Add(new PSNoteProperty("Items", archive.GetEntryCount()));
                    extendedArchives.Add(extendedArchive);
                }

                if (string.IsNullOrEmpty(Name))
                {
                    WriteObject(extendedArchives, true);
                }
                else
                {
                    WildcardWrite(Name, extendedArchives, archive => ((Archive)archive.BaseObject).Name);
                }
            }
        }
    }
}