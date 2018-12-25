using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Sitecore.Data;
using Sitecore.Data.Archiving;
using Sitecore.Security.Accounts;

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

                Archive.RemoveEntries(new ID(archivalId));
            }
            else if (Identity != null)
            {
                var user = User.FromName(Identity.Name, false);
                if (user == null) return;
                if (!ShouldProcess(Identity.ToString(), "Remove items by User")) return;

                Archive.RemoveEntries(user);
            }
            else if (ArchiveItem != null)
            {
                foreach (var item in ArchiveItem)
                {
                    var archivalId = item.ArchivalId;
                    if (!ShouldProcess(item.ItemId.ToString(), "Remove items by ArchiveItem")) return;

                    var archive = ArchiveManager.GetArchive(item.ArchiveName, item.Database);
                    archive.RemoveEntries(new ID(archivalId));
                }
            }
        }
    }
}