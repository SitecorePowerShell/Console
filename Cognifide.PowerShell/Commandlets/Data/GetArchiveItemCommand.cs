using Cognifide.PowerShell.Commandlets.Security;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Security.Accounts;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "ArchiveItem", DefaultParameterSetName = "Everything")]
    [OutputType(typeof (ArchiveEntry))]
    public class GetArchiveItemCommand : BaseCommand
    {
        [Parameter(Mandatory = true, ParameterSetName = "Everything")]
        [Parameter(Mandatory = true, ParameterSetName = "Find by ID")]
        [Parameter(Mandatory = true, ParameterSetName = "Find by User")]
        public Archive Archive { get; set; }

        [Parameter(ParameterSetName = "Find by ID")]
        public ID ItemId { get; set; }

        [Parameter(ParameterSetName = "Find by User")]
        public AccountIdentity Identity { get; set; }

        protected override void ProcessRecord()
        {
            if (Identity != null)
            {
                var user = User.FromName(Identity.Name, false);
                if (user == null) return;

                var entryCount = Archive.GetEntryCountForUser(user);
                WriteObject(Archive.GetEntriesForUser(user, 0, entryCount), true);
            }
            else
            {
                if (!ID.IsNullOrEmpty(ItemId))
                {
                    var archivalId = Archive.GetArchivalId(ItemId);
                    WriteObject(Archive.GetEntries(new ID(archivalId)), true);
                }
                else
                {
                    var entryCount = Archive.GetEntryCount();
                    WriteObject(Archive.GetEntries(0, entryCount), true);
                }
            }
        }
    }
}