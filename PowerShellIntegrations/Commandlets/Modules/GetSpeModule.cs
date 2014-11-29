using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModule")]
    [OutputType(new[] {typeof (Module)},
        ParameterSetName =
            new[] { "Module from Database", "Module from Pipeline", "Module from Path", "Module from ID", "Module from Name"})]
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

        [Parameter(ParameterSetName = "Module from ID", ValueFromPipeline = true )]
        [Parameter(ParameterSetName = "Module from Database", ValueFromPipeline = true, Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [Parameter(ParameterSetName = "Module from Name", ValueFromPipeline = true)]
        public override Database Database { get; set; }

        [Parameter(ParameterSetName = "Module from Name")]
        public string Name { get; set; }

        // hide language as it's not relevant here
        public override string[] Language { get; set; }

        protected override void ProcessRecord()
        {
            IEnumerable<Module> modules = ModuleManager.Modules;
            
            bool databaseDefined = Database != null;
            bool nameDefined = !string.IsNullOrEmpty(Name);
            
            if (databaseDefined)
            {
                modules = WildcardFilter(Database.Name, modules, m => m.Database);
            }
            
            if(nameDefined)
            {
                modules = WildcardFilter(Name, modules, m => m.Name);
            }
            
            if (databaseDefined || nameDefined)
            {
                WriteObject(modules,true);
                return;
            }

            base.ProcessRecord();
        }

        protected override void ProcessItem(Item item)
        {
            WriteObject(ModuleManager.GetItemModule(item),false);
        }
    }
}