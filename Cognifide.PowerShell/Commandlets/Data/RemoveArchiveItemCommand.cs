using Cognifide.PowerShell.Commandlets.Security;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Security.Accounts;
using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Remove, "ArchiveItem", DefaultParameterSetName = "Everything", SupportsShouldProcess = true)]
    public class RemoveArchiveItemCommand : BaseCommand
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
                if (!ShouldProcess(ItemId.ToString(), "Remove items by Item Id")) return;

                WriteVerbose($"Removing item {ItemId} from the archive {Archive.Name}");
                Archive.RemoveEntries(new ID(archivalId));
            }
            else if (Identity != null)
            {
                var user = User.FromName(Identity.Name, false);
                if (user == null) return;
                if (!ShouldProcess(Identity.ToString(), "Remove items by User")) return;

                WriteVerbose($"Removing items for user {Identity} from the archive {Archive.Name}");
                Archive.RemoveEntries(user);
            }
            else if (ArchiveItem != null)
            {
                foreach (var entry in ArchiveItem)
                {
                    var archivalId = entry.ArchivalId;
                    if (!ShouldProcess(entry.ItemId.ToString(), "Remove items by ArchiveItem")) return;

                    var archive = ArchiveManager.GetArchive(entry.ArchiveName, entry.Database);
                    if (archive == null) return;
                    WriteVerbose($"Removing item {entry.ItemId} from the archive {entry.ArchiveName} in database {entry.Database.Name}");
                    archive.RemoveEntries(new ID(archivalId));
                }
            }
        }
    }
}