using System;
using System.Linq;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Integrations.Pipelines
{
    public abstract class PipelineProcessor<TPipelineArgs> where TPipelineArgs : PipelineArgs
    {
        protected abstract string IntegrationPoint { get; }

        protected void Process(TPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            foreach (var libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!libraryItem.HasChildren) continue;

                foreach (var scriptItem in libraryItem.Children.Where(si => si.IsPowerShellScript() && !string.IsNullOrWhiteSpace(si[Templates.Script.Fields.ScriptBody])))
                {
                    if (!RulesUtils.EvaluateRules(scriptItem[Templates.Script.Fields.EnableRule], scriptItem)) continue;

                    using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
                    {
                        session.SetVariable("pipelineArgs", args);

                        try
                        {
                            session.ExecuteScriptPart(scriptItem, false);
                        }
                        catch (Exception ex)
                        {
                            PowerShellLog.Error(
                                $"Error while executing script in {GetType().FullName} pipeline processor.", ex);
                        }
                    }
                }
            }
        }
    }
}