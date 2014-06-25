using System.Collections.Generic;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    /// <summary>
    ///     Cmdlet. Used to list profile cards for certain profile name. Instead of name, an asterisk is accepted - this will
    ///     cause in listing all profile cards.
    ///     <author>Szymon Kuzniak (szymon.kuzniak@cognifide.com)</author>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "AnalyticsProfileCards")]
    [OutputType(new[] {typeof (Item)})]
    public class GetAnalyticsProfileCardsCommand : DatabaseContextBaseCommand
    {
        public const string ProfileCardsHubTemplate =
            GetAnalyticsProfileItemCommand.MarketingCenterProfiles + "/{0}/Profile Cards";

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            var profileCards = new List<Item>();
            foreach (var database in databases)
            {
                Item[] profiles = database.SelectItems(string.Format(ProfileCardsHubTemplate, Name));
                foreach (var profile in profiles)
                {
                    if (profile != null)
                    {
                        profileCards.AddRange(profile.Children);
                    }
                }
            }

            if (string.IsNullOrEmpty(Name))
            {
                WriteError(new ErrorRecord(new InvalidPowerShellStateException("Name parameter is mandatory."),
                    "sitecore_name_parameter_missing",ErrorCategory.InvalidData, null));
            }
            WriteObject(profileCards);
        }
    }
}