using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsCommon.Set, "ItemAcl", SupportsShouldProcess = true)]
    [OutputType(typeof (Item))]
    public class SetItemAclCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true)]
        public AccessRuleCollection AccessRules { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (!this.CanAdmin(item)) { return; }

            if (ShouldProcess(item.GetProviderPath(), "Change access rights"))
            {
                item.Security.SetAccessRules(AccessRules);
            }

            if (PassThru)
            {
                WriteItem(item);
            }

        }
    }
}