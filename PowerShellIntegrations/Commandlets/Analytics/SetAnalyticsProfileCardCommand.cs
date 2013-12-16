using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Analytics
{
    /// <summary>
    ///     Cmdlet. Assigns profile cards basing on their names to provided item. Can be used in pipe.
    ///     <author>Szymon Kuzniak (szymon.kuzniak@cognifide.com)</author>
    /// </summary>
    [Cmdlet("Set", "AnalyticsProfileCard")]
    public class SetAnalyticsProfileCardCommand : DatabaseContextBaseCommand
    {
        /// <summary>
        ///     The name of the analytics profile which contains profile cards.
        /// </summary>
        [Parameter(Position = 0)]
        public string ProfileName { get; set; }

        /// <summary>
        ///     The name of the profile card.
        /// </summary>
        [Parameter(Position = 1)]
        public string ProfileCardName { get; set; }

        /// <summary>
        ///     The item to which the profile card should be added (!not appended.)
        /// </summary>
        [Parameter(ValueFromPipeline = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord(IEnumerable<Database> databases)
        {
            if (Item != null)
            {
                string profileCardPath =
                    string.Format(GetAnalyticsProfileCardsCommand.ProfileCardsHubTemplate + "/{1}", ProfileName,
                        ProfileCardName);
                WriteVerbose(string.Format("Profile card path: [{0}]", profileCardPath));
                Item profileCard = Item.Database.SelectItems(profileCardPath).FirstOrDefault();
                WriteVerbose(string.Format("Profile card [{0}] for profile [{1}] => [{2}]", ProfileCardName, ProfileName,
                    profileCard));
                if (profileCard != null)
                {
                    string profileCardValue = profileCard["Profile Card Value"];
                    WriteVerbose(string.Format("Profile card value => [{0}]", profileCardValue));

                    // each card should be enchanced with profile card name as from unkown reason this is not stored in the profile card XML.
                    profileCardValue = ProcessProfileCardValue(profileCardValue, ProfileCardName);
                    WriteVerbose(string.Format("Processed profile card value => [{0}]", profileCardValue));

                    Item.Edit(
                        args => { Item["__Tracking"] = profileCardValue; });
                }
            }
            else
            {
                WriteVerbose("Item is null.");
            }
        }

        private static string ProcessProfileCardValue(string profileCard, string profileCardName)
        {
            string result = profileCard;

            var doc = new XmlDocument();
            doc.LoadXml(profileCard);

            if (doc.DocumentElement == null || doc.GetElementsByTagName("profile").Count == 0 ||
                doc.GetElementsByTagName("profile")[0] == null)
            {
                return result;
            }

            XmlNode xmlNode = doc.GetElementsByTagName("profile")[0];
            if (xmlNode.Attributes != null)
            {
                XmlAttribute presetAttribute = xmlNode.Attributes["presets"] ?? doc.CreateAttribute("presets");

                presetAttribute.Value = profileCardName.ToLower() + "|100||";
                xmlNode.Attributes.Append(presetAttribute);
            }

            return doc.InnerXml;
        }
    }
}