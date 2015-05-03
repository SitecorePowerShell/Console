using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;
using AuthorizationManager = Sitecore.Security.AccessControl.AuthorizationManager;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsDiagnostic.Test, "ItemAcl")]
    [OutputType(typeof(AccessRule))]
    public class TestItemAclCommand : BaseItemAclCommand, IDynamicParameters
    {
        public override string Filter { get; set; }        

        protected override void ProcessItem(Item item)
        {
            AccessRight accessRight;
            if (!this.TryGetAccessRight(out accessRight, true))
            {
                WriteObject(false);
            }
            else
            {
                WriteObject(AuthorizationManager.IsAllowed(item, accessRight, Identity));
            }
        }

        public TestItemAclCommand()
        {
            AddDynamicParameter<string>("AccessRight", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = true,
                Position = 1
            }, new ValidateSetAttribute(WellKnownRights));            
        }
    }
}