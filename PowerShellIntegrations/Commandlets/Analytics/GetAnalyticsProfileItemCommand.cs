using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    /// <summary>
    ///     Cmdlet. Gets analytics profiles by name. Supports wildcards. By default profiles are taken from all databases.
    ///     <author>Szymon Kuzniak (szymon.kuzniak@cognifide.com)</author>
    /// </summary>
    [Cmdlet("Get", "AnalyticsProfileItem")]
    [OutputType(new[] { typeof(Item) })]
    public class GetAnalyticsProfileItemCommand : DatabaseContextBaseCommand
    {
        public const string MarketingCenterProfiles = "/sitecore/system/Marketing Center/Profiles";

        [ValidatePattern("[\\*\\?\\[\\]\\-0-9a-zA-Z_]+")]
        [Parameter(Position = 0)]
        public override string Name { get; set; }

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            var profiles = new List<Item>();
            foreach (Database database in databases)
            {
                Item profilesHub = database.SelectItems(MarketingCenterProfiles).FirstOrDefault();
                if (profilesHub != null)
                {
                    profiles.AddRange(profilesHub.Children);
                }
            }

            if (string.IsNullOrEmpty(Name))
            {
                WriteObject(profiles);
            }
            else
            {
                WildcardWrite(Name, profiles, profile => profile.Name);
            }
        }
    }
}