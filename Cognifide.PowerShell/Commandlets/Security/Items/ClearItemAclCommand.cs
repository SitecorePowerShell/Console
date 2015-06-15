using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Clear, "ItemAcl", SupportsShouldProcess = true)]
    [OutputType(typeof (Item))]
    public class ClearItemAclCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (!this.CanAdmin(item)) { return; }

            if (ShouldProcess(item.GetProviderPath(),"Clear access rights"))
            {
                item.Security.SetAccessRules(new AccessRuleCollection());
            }

            if (PassThru)
            {
                WriteItem(item);
            }

        }
    }
}