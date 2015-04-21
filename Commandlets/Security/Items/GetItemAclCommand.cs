using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Get, "ItemAcl", SupportsShouldProcess = true)]
    [OutputType(typeof (bool), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class GetItemAclCommand : BaseItemCommand
    {
        protected override void ProcessItem(Item item)
        {    
            WriteObject(item.Security.GetAccessRules(),true);
            //item.Security.SetAccessRules();
        }
    }
}