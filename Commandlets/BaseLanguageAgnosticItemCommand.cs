using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseLanguageAgnosticItemCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Mandatory = true)]
        public virtual Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory=true)]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        public virtual string Id { get; set; }

        [Parameter(ParameterSetName = "Item from ID")]
        public virtual Database Database { get; set; }

        protected override void ProcessRecord()
        {
            var sourceItem = FindItemFromParameters(Item, Path, Id, null, Database);

            if (sourceItem == null)
            {
                WriteError(
                    new ErrorRecord(
                        new ObjectNotFoundException(
                            "Item not found."),
                        "sitecore_item_not_found", ErrorCategory.InvalidData, null));
                return;
            }

            ProcessItem(sourceItem);
        }

        protected abstract void ProcessItem(Item item);

    }
}