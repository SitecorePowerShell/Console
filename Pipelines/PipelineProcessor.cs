using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Pipelines;

namespace Cognifide.PowerShell.Pipelines
{
    public abstract class PipelineProcessor<TPipelineArgs> where TPipelineArgs : PipelineArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PipelineProcessor{TPipelineArgs}"/> class.
        /// </summary>
        /// <remarks>Used configuration example from this article:
        /// http://www.partechit.nl/en/blog/2014/09/configurable-pipeline-processors-and-event-handlers
        /// </remarks>
        protected PipelineProcessor()
        {
            Configuration = new Dictionary<string, string>();
        }

        protected void Process(TPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            Assert.IsNotNullOrEmpty(Configuration["libraryId"], "The configuration setting 'libraryId' must exist.");

            var libraryId = new ID(Configuration["libraryId"]);

            var db = Factory.GetDatabase("master");
            var libraryItem = db.GetItem(libraryId);
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
                        session.ExecuteScriptPart(script, false);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex.Message, this);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the processor configuration.
        /// </summary>
        protected Dictionary<string, string> Configuration { get; private set; }

        /// <summary>
        /// Configures the processor.
        /// </summary>
        /// <param name="node">The node.</param>
        public void Config(XmlNode node)
        {
            Configuration.Add(node.Name, node.InnerText);
        }
    }
}