using System.Globalization;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.Integrations.Workflows
{
    public class ScriptAction
    {
        public void Process(WorkflowPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            var processorItem = args.ProcessorItem;
            if (processorItem == null)
            {
                return;
            }
            var actionItem = processorItem.InnerItem;

            var dataItem = args.DataItem;

            if (string.IsNullOrEmpty(actionItem[ScriptItemFieldNames.Script]))
            {
                return;
            }

            var scriptItem = actionItem.Database.GetItem(new ID(actionItem[ScriptItemFieldNames.Script]));

            if (RulesUtils.EvaluateRules(actionItem[ScriptItemFieldNames.EnableRule], dataItem) &&
                RulesUtils.EvaluateRules(scriptItem[ScriptItemFieldNames.EnableRule], dataItem))
            {
                var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
                str.Append("id", dataItem.ID.ToString());
                str.Append("db", dataItem.Database.Name);
                str.Append("lang", dataItem.Language.Name);
                str.Append("ver", dataItem.Version.Number.ToString(CultureInfo.InvariantCulture));
                str.Append("scriptId", scriptItem.ID.ToString());
                str.Append("scriptDb", scriptItem.Database.Name);
                Context.ClientPage.ClientResponse.Broadcast(
                    SheerResponse.ShowModalDialog(str.ToString(), "400", "220", "PowerShell Script Results", false),
                    "Shell");
            }
        }
    }
}