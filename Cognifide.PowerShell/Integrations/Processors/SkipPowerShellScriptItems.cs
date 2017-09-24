using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.ExpandInitialFieldValue;

namespace Cognifide.PowerShell.Integrations.Processors
{
    public class SkipPowerShellScriptItems : ExpandInitialFieldValueProcessor
    {
        public override void Process(ExpandInitialFieldValueArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            if (args.TargetItem.IsPowerShellScript())
            {
                args.AbortPipeline();
            }
        }
    }
}
