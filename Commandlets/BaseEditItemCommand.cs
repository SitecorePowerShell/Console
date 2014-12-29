using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Cognifide.PowerShell.Extensions;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseEditItemCommand : BaseGovernanceCommand
    {
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected abstract void EditItem(Item item);

        protected override void ProcessItemInUserContext(Item item)
        {
            if (!this.CanChangeReadOnly(item)) { return; }
            if (!this.CanWrite(item)) { return; }
            if (!this.CanChangeLock(item)) { return; }

            item.Editing.BeginEdit();
            EditItem(item);
            item.Editing.EndEdit();

            if (PassThru) { WriteItem(item); }
        }
    }
}