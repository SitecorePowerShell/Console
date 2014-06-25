using System;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet(VerbsData.Expand, "Token")]
    [OutputType(new[] {typeof (Item)})]
    public class ExpandTokenCommand : BaseCommand
    {
        private static readonly MasterVariablesReplacer TokenReplacer = Factory.GetMasterVariablesReplacer();

        [Parameter(Position = 0, Mandatory = true, ValueFromPipeline = true)]
        public Item Item { get; set; }

        protected override void ProcessRecord()
        {
            Item.Editing.BeginEdit();
            try
            {
                TokenReplacer.ReplaceItem(Item);
                Item.Editing.EndEdit();
            }
            catch (Exception ex)
            {
                Item.Editing.CancelEdit();
                WriteError(new ErrorRecord(ex,"sitecore_token_expander_error",ErrorCategory.NotSpecified, Item));
            }

            WriteItem(Item);
        }
    }
}