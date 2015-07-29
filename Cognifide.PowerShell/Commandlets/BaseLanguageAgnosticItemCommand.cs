using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets
{
    public abstract class BaseLanguageAgnosticItemCommand : BaseShellCommand
    {
        private static readonly string[] databases = Factory.GetDatabaseNames();

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Item from Pipeline", Mandatory = true, Position = 0)]
        public virtual Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path", Mandatory = true, Position = 0)]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID", Mandatory = true)]
        public virtual string Id { get; set; }

        [ValidateSet("*")]
        [Parameter(ParameterSetName = "Item from ID")]
        public virtual string Database { get; set; }

        protected override void ProcessRecord()
        {
            Factory.GetDatabase(Database);
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

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("Database", databases);

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }

    }
}