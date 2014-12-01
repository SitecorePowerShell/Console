using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModuleFeatureRoot")]
    [OutputType(new[] {typeof (Item)}, ParameterSetName = new[] { "By Feature Name" })]
    public class GetSpeModuleFeatureRoot : BaseCommand, IDynamicParameters
    {

        protected override void ProcessRecord()
        {
            string feature;
            if (TryGetParameter("Feature", out feature))
            {
                if (!String.IsNullOrEmpty(feature))
                {
                    ModuleManager.GetFeatureRoots(feature).ForEach(WriteItem);
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