using System.Globalization;
using System.Linq;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Sitecore.Workflows.Simple;

namespace Cognifide.PowerShell.SitecoreIntegrations.Workflows
{
    public class ScriptAction
    {
        public void Process(WorkflowPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            ProcessorItem processorItem = args.ProcessorItem;
            if (processorItem == null)
            {
                return;
            }
            Item actionItem = processorItem.InnerItem;

            Item dataItem = args.DataItem;

            if (string.IsNullOrEmpty(actionItem["Script"]))
            {
                return;
            }

            Item scriptItem = actionItem.Database.GetItem(new ID(actionItem[ScriptItemFieldNames.Script]));

            if (EvaluateRules(actionItem[ScriptItemFieldNames.EnableRule], dataItem) &&
                EvaluateRules(scriptItem[ScriptItemFieldNames.EnableRule], dataItem))
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

        public static bool EvaluateRules(string strRules, Item contextItem)
        {
            if (string.IsNullOrEmpty(strRules) || strRules.Length < 20)
            {
                return true;
            }
            // hacking the rules xml
            RuleList<RuleContext> rules = RuleFactory.ParseRules<RuleContext>(Factory.GetDatabase("master"), strRules);
            var ruleContext = new RuleContext
            {
                Item = contextItem
            };

            return !rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext));
        }

    }
}