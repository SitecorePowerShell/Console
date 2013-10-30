using System;
using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Indexing;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
#pragma warning disable 612, 618
    [Cmdlet("Get", "Index")]
    [OutputType(new[] {typeof (Index)})]
    public class GetIndexCommand : DatabaseContextBaseCommand
    {
        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            if (String.IsNullOrEmpty(Name))
            {
                foreach (var database in databases)
                {
                    WriteObject(database.Indexes, true);
                }
            }
            else
            {
                foreach (var database in databases)
                {
                    var indices = new List<Index>(database.Indexes.Count);
                    for (int i = 0; i < database.Indexes.Count; i++)
                    {
                        indices.Add(database.Indexes[i]);
                    }
                    WildcardWrite(Name, indices, index => index.Name);
                }
            }
        }
    }
#pragma warning restore 612, 618
}