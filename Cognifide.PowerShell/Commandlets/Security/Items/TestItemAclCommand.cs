using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.AccessControl;
using AuthorizationManager = Sitecore.Security.AccessControl.AuthorizationManager;

namespace Cognifide.PowerShell.Commandlets.Security.Items
{
    [Cmdlet(VerbsDiagnostic.Test, "ItemAcl")]
    [OutputType(typeof(bool))]
    public class TestItemAclCommand : BaseItemAclCommand, IDynamicParameters
    {

        public override string Filter { get; set; }

        [Parameter(ParameterSetName = "Item from Pipeline", Mandatory = true, ValueFromPipeline = true)]
        [Parameter(ParameterSetName = "Account ID, Item from Pipeline", Mandatory = true, ValueFromPipeline = true)]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from Path", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        [Parameter(ParameterSetName = "Account ID, Item from ID", Mandatory = true)]
        public override string Id { get; set; }

        [Parameter(ParameterSetName = "Account ID, Item from ID")]
        public override Database Database { get; set; }

        protected override void ProcessItem(Item item)
        {
            AccessRight accessRight;
            WriteObject(this.TryGetAccessRight(out accessRight, true) &&
                        AuthorizationManager.IsAllowed(item, accessRight, Identity));
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