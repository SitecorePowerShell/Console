using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModuleFeatureRoot")]
    [OutputType(typeof (Item), ParameterSetName = new[] {"By Feature Name", "Module from Pipeline"})]
    public class GetSpeModuleFeatureRoot : BaseCommand
    {
        public static readonly string[] Features = IntegrationPoints.Libraries.Keys.ToArray();

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Module from Pipeline")]
        public Module Module { get; set; }

        [Parameter]
        public SwitchParameter ReturnPath { get; set; }

        [AutocompleteSet("Features")]
        [Parameter(Mandatory = true, Position = 0)]
        public string Feature { get; set; }

        protected override void ProcessRecord()
        {
            if (!string.IsNullOrEmpty(Feature))
            {
                if (Module != null)
                {
                    if (ReturnPath)
                    {
                        WriteObject(Module.GetProviderFeaturePath(Feature));
                    }
                    else
                    {
                        WriteItem(Module.GetFeatureRoot(Feature));
                    }
                    return;
                }

                if (ReturnPath)
                {
                    ModuleManager.Modules.ForEach(m => WriteObject(m.GetProviderFeaturePath(Feature)));
                }
                else
                {
                    ModuleManager.GetFeatureRoots(Feature).ForEach(WriteItem);
                }
            }
        }
    }
}