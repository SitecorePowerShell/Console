using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Get, "ItemAcl")]
    [OutputType(typeof(AccessRule))]
    public class GetItemAclCommand : BaseItemAclCommand
    {

        protected override void ProcessItem(Item item)
        {
            List<AccessRule> accessRights = item.Security.GetAccessRules().ToList();
            if (ParameterSetName.StartsWith("Account ID"))
            {
                accessRights.Where(ar => ar.Account.Name.Equals(Identity.Name)).ForEach(WriteObject);
            }
            else if (ParameterSetName.StartsWith("Account Filter"))
            {
                WildcardWrite(Filter, accessRights, ar => ar.Account.Name);
            }
            else
            {
                WriteObject(accessRights, true);
            }
        }
    }
}