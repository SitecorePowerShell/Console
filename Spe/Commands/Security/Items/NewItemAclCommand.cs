using System.Management.Automation;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;
using Spe.Core.Extensions;
using Spe.Core.Validation;

namespace Spe.Commands.Security.Items
{
    [Cmdlet(VerbsCommon.New, "ItemAcl")]
    [OutputType(typeof (AccessRule))]
    public class NewItemAclCommand : BaseCommand
    {
        public static readonly string[] WellKnownRights = BaseItemAclCommand.WellKnownRights;

        [Alias("User")]
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public virtual AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [AutocompleteSet(nameof(WellKnownRights))]
        public string AccessRight { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public virtual PropagationType PropagationType { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public virtual SecurityPermission SecurityPermission { get; set; }

        protected override void ProcessRecord()
        {
            if (!this.TryParseAccessRight(AccessRight, out var accessRight)) return;

            var account = this.GetAccountFromIdentity(Identity);

            var accessRule = AccessRule.Create(account, accessRight, PropagationType, SecurityPermission);
            WriteObject(accessRule);
        }
    }
}