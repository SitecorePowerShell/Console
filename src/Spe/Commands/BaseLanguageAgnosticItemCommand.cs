using System.Data;
using System.Management.Automation;
using Sitecore.Data.Items;
using Spe.Commands.Interactive;
using Spe.Core.Validation;

namespace Spe.Commands
{
    public abstract class BaseLanguageAgnosticItemCommand : BaseShellCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Mandatory = true, Position = 0)]
        public virtual Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        public virtual string Id { get; set; }

        [AutocompleteSet(nameof(Databases))]
        [Parameter(ParameterSetName = "Item from ID")]
        public virtual string Database { get; set; }

        protected override void ProcessRecord()
        {
            var sourceItem = FindItemFromParameters(Item, Path, Id, null, Database);

            if (sourceItem == null)
            {
                WriteError(typeof(ObjectNotFoundException), "Cannot find item to perform the operation on.", ErrorIds.ItemNotFound, ErrorCategory.InvalidData, null);
                return;
            }

            ProcessItem(sourceItem);
        }

        protected abstract void ProcessItem(Item item);
    }
}