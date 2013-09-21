using System;
using System.Management.Automation;

using Sitecore.Configuration;

using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet("Expand", "Tokens", DefaultParameterSetName = "Item")]
    [OutputType(new[] { typeof(Item) })]
    public class ExpandTokensCommand : BaseCommand
    {
        private static readonly MasterVariablesReplacer TokenReplacer = Factory.GetMasterVariablesReplacer();

        [Parameter(Position = 0, Mandatory = true)]
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
                throw ex;
            }

            PSObject psobj = ItemShellExtensions.GetPsObject(SessionState, Item);
            WriteObject(psobj, false);
        }
    }
}