using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Configuration;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseLanguageAgnosticItemCommand : BaseShellCommand
    {
        public static readonly string[] Databases = Factory.GetDatabaseNames().Where(name => name != "filesystem").ToArray();

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Mandatory = true, Position = 0)]
        public virtual Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        public virtual string Id { get; set; }

        [AutocompleteSet("Databases")]
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