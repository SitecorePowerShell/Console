using Sitecore.Diagnostics;
using Sitecore.Pipelines.ExpandInitialFieldValue;
using Spe.Core.Extensions;

namespace Spe.Integrations.Processors
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
