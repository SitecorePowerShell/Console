using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;

namespace Cognifide.PowerShell.Shell.Commands
{
    public abstract class DatabaseContextBaseCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 1)]
        public virtual Database Database { get; set; }

        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 0)]
        public virtual string Name { get; set; }

        protected override void ProcessRecord()
        {
            var databases = new List<Database>();

            if (Database != null)
            {
                databases.AddRange(WildcardFilter(Database.Name, Factory.GetDatabases(), db => db.Name));
            }

            if (databases.Count == 0 || Database == null)
            {
                databases.AddRange(Factory.GetDatabases());
            }

            ProcessRecord(databases);
        }

        protected abstract void ProcessRecord(IEnumerable<Database> databases);
    }
}