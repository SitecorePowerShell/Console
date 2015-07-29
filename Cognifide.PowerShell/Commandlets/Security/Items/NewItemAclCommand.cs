using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.New, "ItemAcl")]
    [OutputType(typeof (AccessRule))]
    public class NewItemAclCommand : BaseCommand
    {
        [Alias("User")]
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public virtual AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateSet("*")]
        public string AccessRight { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public virtual PropagationType PropagationType { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public virtual SecurityPermission SecurityPermission { get; set; }

        protected override void ProcessRecord()
        {
            AccessRight accessRight; 
            
            if (!this.TryParseAccessRight(AccessRight, out accessRight)) return;

            Account account = this.GetAccountFromIdentity(Identity);

            var accessRule = AccessRule.Create(account, accessRight, PropagationType, SecurityPermission);
            WriteObject(accessRule);
        }

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("AccessRight", BaseItemAclCommand.WellKnownRights);

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }

    }
}