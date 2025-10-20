using System;
using System.Linq;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Sitecore.Security.Accounts;
using Sitecore.Shell.Applications.ContentEditor.Gutters;
using Spe.Core.Diagnostics;
using Spe.Core.Host;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Core.Rules;

public class ScriptResultBoolValueCondition<T> : WhenCondition<T> where T : RuleContext
{
    public ID ScriptId { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull(ruleContext, "ruleContext");
        var item = ruleContext.Item;

        if (item == null || ScriptId == ID.Null)
        {
            return false;
        }
        var scriptResult = GetScriptResult(item);
        return scriptResult;
    }

    private bool GetScriptResult(Item item)
    {
        return SpeTimer.Measure("bool script value in rule execution", true, () =>
        {
            // the rule is only supposed to be used in CM - therefore defaulting to "master" database
            var db = Factory.GetDatabase("master");
            var scriptItem = db.GetItem(ScriptId);
            PowerShellLog.Info($"[Rule] Executing script {scriptItem.ID} for Context User {Context.User.Name}.");
            return RunRuleScript(item, scriptItem);
        });
    }

    private bool RunRuleScript(Item contextItem, Item scriptItem)
    {
        try
        {
            // Create a new session for running the script.
            var session = ScriptSessionManager.GetSession(scriptItem[Templates.Script.Fields.PersistentSessionId],
                IntegrationPoints.ScriptInRuleFeature);

            // We will need the item variable in the script.
            session.SetItemLocationContext(contextItem);
            session.SetExecutedScript(scriptItem);

            // Any objects written to the pipeline in the script will be returned.
            var result = session.ExecuteScriptPart(scriptItem, false).Last();
            return (result?.ToString() ?? string.Empty).Equals("true", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            PowerShellLog.Error(
                $"Error while invoking script '{scriptItem?.Paths.Path}' for Script Bool Result Rule.", ex);
        }

        return false;
    }

}        
