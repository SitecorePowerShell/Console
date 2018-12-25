using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Security.Accounts;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsData.Restore, "ArchiveItem", DefaultParameterSetName = "Everything", SupportsShouldProcess = true)]
    public class RestoreArchiveItemCommand : BaseCommand
    {
        [Parameter(Mandatory = true, ParameterSetName = "Everything")]
        [Parameter(Mandatory = true, ParameterSetName = "Find by ID")]
        [Parameter(Mandatory = true, ParameterSetName = "Find by User")]
        public Archive Archive { get; set; }

        [Parameter(ParameterSetName = "Find by ID")]
        public ID ItemId { get; set; }

        [Parameter(ParameterSetName = "Find by User", ValueFromPipeline = true)]
        public AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Find by ArchiveItem", ValueFromPipeline = true)]
        public ArchiveEntry[] ArchiveItem { get; set; }

        protected override void ProcessRecord()
        {
            if (!ID.IsNullOrEmpty(ItemId))
            {
                var archivalId = Archive.GetArchivalId(ItemId);
                if (!ShouldProcess(ItemId.ToString(), "Restore items by Item Id")) return;

                Archive.RestoreItem(archivalId);
            }
            else if (Identity != null)
            {
                var user = User.FromName(Identity.Name, false);
                if (user == null) return;
                if (!ShouldProcess(Identity.ToString(), "Restore items by User")) return;

                var entryCount = Archive.GetEntryCountForUser(user);
                var entries = Archive.GetEntriesForUser(user, 0, entryCount);
                entries.ForEach(item => Archive.RestoreItem(item.ArchivalId));
            }
            else if (ArchiveItem != null)
            {
                foreach (var item in ArchiveItem)
                {
                    var archivalId = item.ArchivalId;
                    if (!ShouldProcess(item.ItemId.ToString(), "Restore items by ArchiveItem")) return;

                    var archive = ArchiveManager.GetArchive(item.ArchiveName, item.Database);
                    archive.RestoreItem(archivalId);
                }
            }
        }
    }
}