using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.New, "ItemAcl")]
    [OutputType(typeof (AccessRule))]
    public class NewItemAclCommand : BaseCommand, IDynamicParameters
    {
        [Alias("User")]
        [Parameter(Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public virtual AccountIdentity Identity { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public virtual PropagationType PropagationType { get; set; }

        [Parameter(Mandatory = true, Position = 3)]
        public virtual SecurityPermission SecurityPermission { get; set; }

        protected override void ProcessRecord()
        {
            AccessRight accessRight; 
            
            if (!this.TryGetAccessRight(out accessRight, true)) return;

            Account account = this.GetAccountFromIdentity(Identity);

            var accessRule = AccessRule.Create(account, accessRight, PropagationType, SecurityPermission);
            WriteObject(accessRule);
        }

        public NewItemAclCommand()
        {
            AddDynamicParameter<string>("AccessRight", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = true,
                Position = 1
            }, new ValidateSetAttribute(BaseItemAclCommand.WellKnownRights));            
        }
    }
}