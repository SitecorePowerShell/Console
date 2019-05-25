using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commandlets.Security.Items
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