using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Modules;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;

namespace Cognifide.PowerShell.Pipelines
{
    public abstract class PipelineProcessor<TPipelineArgs> where TPipelineArgs : PipelineArgs
    {
        protected abstract string IntegrationPoint { get; }

        protected void Process(TPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            foreach (Item libraryItem in ModuleManager.GetFeatureRoots(IntegrationPoint))
            {
                if (!libraryItem.HasChildren) return;

                foreach (var scriptItem in libraryItem.Children.ToList())
                {
                    using (var session = new ScriptSession(ApplicationNames.Default))
                    {
                        var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                            ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                            : String.Empty;
                        session.SetVariable("args", args);

                        try
                        {
                            session.SetExecutedScript(scriptItem);
                            session.ExecuteScriptPart(script, false);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex.Message, this);
                        }
                    }
                }
            }
        }
    }
}