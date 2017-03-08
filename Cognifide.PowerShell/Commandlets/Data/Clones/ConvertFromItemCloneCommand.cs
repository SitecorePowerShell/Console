using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.Commandlets.Data.Clones
{
    [Cmdlet(VerbsData.ConvertFrom, "ItemClone", SupportsShouldProcess = true)]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class ConvertFromItemCloneCommand : BaseLanguageAgnosticItemCommand
    {

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(),
                $"Convert item clone {(Recurse ? "and its children " : string.Empty)}to full item"))
            {
                Unclone(item);
            }
        }

        private void Unclone(Item item)
        {
            if (item.IsClone)
            {
                var clone = new CloneItem(item);
                var fullItem = clone.Unclone();
                if (PassThru)
                {
                    WriteItem(fullItem);
                }
            }
            else
            {
                if (!item.IsItemClone)
                {
                    WriteError(typeof(InvalidTypeException), "The specified item is not a clone.", ErrorIds.InvalidItemType, ErrorCategory.InvalidType, null);
                }
            }
            if (Recurse)
            {
                foreach (Item child in item.Children)
                {
                    Unclone(child);
                }
            }
        }
    }
}