using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "Database")]
    [OutputType(typeof (Database))]
    public class GetDatabaseCommand : BaseCommand
    {
        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(ValueFromPipeline = true, Position = 0)]
        public string Name { get; set; }

        [Parameter(ValueFromPipeline = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            if (Item != null)
            {
                WriteObject(Item.Database, true);
            }
            else
            {
                if (Name != null)
                {
                    WildcardWrite(Name, Factory.GetDatabases(), db => db.Name);
                }
                else
                    WriteObject(Factory.GetDatabases(), true);
            }
        }
    }
}