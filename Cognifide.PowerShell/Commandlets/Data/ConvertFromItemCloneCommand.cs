using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet("ConvertFrom", "ItemClone", SupportsShouldProcess = true)]
    [OutputType(typeof (Item), ParameterSetName = new[] {"Item from Pipeline", "Item from Path", "Item from ID"})]
    public class ConvertFromItemCloneCommand : BaseLanguageAgnosticItemCommand
    {
        protected override void ProcessItem(Item item)
        {
            if (!item.IsItemClone)
            {
                WriteError(typeof(ArgumentException), "The specified item is not a clone.", ErrorIds.InvalidItemType, ErrorCategory.InvalidArgument, null);
                return;
            }
            if (ShouldProcess(item.GetProviderPath(), "Convert item clone to full item"))
            {
                var clone = new CloneItem(item);
                WriteItem(clone.Unclone());
            }
        }
    }
}