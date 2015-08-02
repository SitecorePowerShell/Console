using System.Collections.Generic;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModule")]
    [OutputType(typeof (Module),
        ParameterSetName =
            new[]
            {"Module from Database", "Module from Pipeline", "Module from Path", "Module from ID", "Module from Name"})]
    public class GetSpeModule : BaseItemCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Module from Pipeline", Mandatory = true)]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Module from Path", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Module from ID", Mandatory = true)]
        public override string Id { get; set; }

        [AutocompleteSet("Databases")]
        [Parameter(ParameterSetName = "Module from ID", ValueFromPipeline = true, Mandatory = true)]
        [Parameter(ParameterSetName = "Module from Database", ValueFromPipeline = true, Mandatory = true,
            ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = "Module from Name", ValueFromPipeline = true)]
        public override string Database { get; set; }

        [Parameter(ParameterSetName = "Module from Name", Mandatory = true)]
        public string Name { get; set; }

        // hide language as it's not relevant here
        public override string[] Language { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<Module> modules = ModuleManager.Modules;

            var databaseDefined = Database != null && string.IsNullOrEmpty(Id);
            var nameDefined = !string.IsNullOrEmpty(Name);

            if (databaseDefined)
            {
                modules = WildcardFilter(Database, modules, m => m.Database);
            }

            if (nameDefined)
            {
                modules = WildcardFilter(Name, modules, m => m.Name);
            }

            if (databaseDefined || nameDefined)
            {
                WriteObject(modules, true);
                return;
            }

            base.ProcessRecord();
        }

        protected override void ProcessItem(Item item)
        {
            WriteObject(ModuleManager.GetItemModule(item), false);
        }
    }
}