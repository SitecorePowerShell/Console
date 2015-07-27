using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Modules;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Modules
{
    [Cmdlet(VerbsCommon.Get, "SpeModuleFeatureRoot")]
    [OutputType(typeof (Item), ParameterSetName = new[] {"By Feature Name", "Module from Pipeline"})]
    public class GetSpeModuleFeatureRoot : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            ParameterSetName = "Module from Pipeline")]
        public Module Module { get; set; }

        [Parameter]
        public SwitchParameter ReturnPath { get; set; }

        [ValidateSet("*")]
        [Parameter(Mandatory = true, Position = 0)]
        public string Feature { get; set; }

        protected override void ProcessRecord()
        {
            string feature;
            var featureDefined = TryGetParameter("Feature", out feature);
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

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("Feature", IntegrationPoints.Libraries.Keys.ToArray());

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }
    }
}