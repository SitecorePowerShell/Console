using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModuleFeatureRoot")]
    [OutputType(new[] {typeof (Item)}, ParameterSetName = new[] { "By Feature Name" })]
    public class GetSpeModuleFeatureRoot : BaseCommand
    {

        [Parameter(ParameterSetName = "By Feature Name", Mandatory = true, Position = 0)]
        public string Feature { get; set; }

        protected override void ProcessRecord()
        {
            ModuleManager.GetFeatureRoots(Feature).ForEach(WriteItem);
        }
    }
}