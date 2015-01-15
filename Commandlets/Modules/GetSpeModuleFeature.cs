using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Modules;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModuleFeatureRoot")]
    [OutputType(new[] { typeof(Item) }, ParameterSetName = new[] { "By Feature Name", "Module from Pipeline" })]
    public class GetSpeModuleFeatureRoot : BaseCommand, IDynamicParameters
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, ParameterSetName = "Module from Pipeline")]
        public Module Module { get; set; }

        [Parameter]
        public SwitchParameter ReturnPath { get; set; }

        protected override void ProcessRecord()
        {
            string feature;
            bool featureDefined = TryGetParameter("Feature", out feature);
            if (Module != null)
            {
                if (ReturnPath)
                {
                    WriteObject(Module.GetProviderFeaturePath(feature));
                }
                else
                {
                    WriteItem(Module.GetFeatureRoot(feature));
                }
                return;
            }
            if (featureDefined)
            {
                if (!String.IsNullOrEmpty(feature))
                {
                    if (ReturnPath)
                    {
                        ModuleManager.Modules.ForEach(m => WriteObject(m.GetProviderFeaturePath(feature)));
                    }
                    else
                    {
                        ModuleManager.GetFeatureRoots(feature).ForEach(WriteItem);
                    }
                }
            }
        }

        public GetSpeModuleFeatureRoot()
        {
            AddDynamicParameter<string>("Feature", new Attribute[]
            {
                new ParameterAttribute
                {
                    ParameterSetName = ParameterAttribute.AllParameterSets,
                    Mandatory = true,
                    Position = 0
                },
                new ValidateSetAttribute(IntegrationPoints.Libraries.Keys.ToArray())
            });
        }
    }
}