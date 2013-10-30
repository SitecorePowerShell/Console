using System;
using System.Linq;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Rules;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands.MenuItems
{
    [Serializable]
    public class ExecutePowerShellScript : Command
    {
        public override void Execute(CommandContext context)
        {
            string scriptId = context.Parameters["script"];
            string scriptDb = context.Parameters["scriptDb"];
            Item scriptItem = Factory.GetDatabase(scriptDb).GetItem(new ID(scriptId));

            string showResults = scriptItem[ScriptItemFieldNames.ShowResults];
            string itemId = string.Empty;
            string itemDb = string.Empty;
            string itemLang = string.Empty;
            string itemVer = string.Empty;

            if (context.Items.Length > 0)
            {
                itemId = context.Items[0].ID.ToString();
                itemDb = context.Items[0].Database.Name;
                itemLang = context.Items[0].Language.Name;
                itemVer = context.Items[0].Version.Number.ToString();
            }

            var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            str.Append("id", itemId);
            str.Append("db", itemDb);
            str.Append("lang", itemLang);
            str.Append("ver", itemVer);
            str.Append("scriptId", scriptId);
            str.Append("scriptDb", scriptDb);
            str.Append("autoClose", showResults);
            Context.ClientPage.ClientResponse.Broadcast(
                SheerResponse.ShowModalDialog(str.ToString(), "400", "220", "PowerShell Script Results", false),
                "Shell");
        }


        public override CommandState QueryState(CommandContext context)
        {
/*
            if (context.Items.Length == 1)
            {
            if (!EvaluateRules(scriptItem["ShowRule"], context.Items[0]))
            {
                continue;
            }

            menuItem.Disabled = !EvaluateRules(scriptItem["EnableRule"], context.Items[0]);
            }
*/
            return base.QueryState(context);
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