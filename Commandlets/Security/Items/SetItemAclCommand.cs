using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Security.AccessControl;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Set, "ItemAcl", SupportsShouldProcess = true)]
    [OutputType(typeof (Item))]
    public class SetItemAclCommand : BaseItemCommand
    {
        [Parameter(ParameterSetName = "Set on Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Set on Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Set on Item from Pipeline", Mandatory = true)]
        public AccessRuleCollection AccessRules { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (!this.CanAdmin(item)) { return; }

            if (ShouldProcess(item.GetProviderPath(),"Change access rights"))
            {
                if (AccessRules is AccessRuleCollection)
                {
                    item.Security.SetAccessRules(AccessRules as AccessRuleCollection);
                }
                else
                {
                    var newAccessRuleCollection = new AccessRuleCollection();
                    newAccessRuleCollection.AddRange(AccessRules);
                    item.Security.SetAccessRules(newAccessRuleCollection);
                }
            }
        }
    }
}