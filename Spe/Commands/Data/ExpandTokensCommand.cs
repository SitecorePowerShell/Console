using System;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Spe.Core.Utility;

namespace Spe.Commands.Data
{
    [Cmdlet(VerbsData.Expand, "Token", SupportsShouldProcess = true)]
    [OutputType(typeof (Item))]
    public class ExpandTokenCommand : BaseItemCommand
    {
        private static readonly MasterVariablesReplacer TokenReplacer = Factory.GetMasterVariablesReplacer();

        protected override void ProcessItem(Item item)
        {
            if (!ShouldProcess(item.GetProviderPath(), "Expand tokens")) return;

            Item.Editing.BeginEdit();
            try
            {
                TokenReplacer.ReplaceItem(Item);
                Item.Editing.EndEdit();
            }
            catch (Exception ex)
            {
                Item.Editing.CancelEdit();
                WriteError(ex.GetType(), "Cannot complete operation.", ErrorIds.InvalidOperation, ErrorCategory.NotSpecified, Item);
            }

            WriteItem(Item);
        }
    }
}