using System;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellBooleanScriptRule<T> : StringOperatorCondition<T> where T : RuleContext
    {
        public ID ScriptId { get; set; }
        protected override bool Execute(T ruleContext)
        {
            var ruleResponse = false;
            Assert.IsNotNull(ruleContext, "RuleContext is null");
            try
            {
                var scriptItem = Sitecore.Context.Database.GetItem(ScriptId);
                if (scriptItem.InheritsFrom(Templates.Script.Id))
                {
                    var scriptItemField = scriptItem.Fields[Templates.Script.Fields.ScriptBody];
                    using (ScriptSession scriptSession = ScriptSessionManager.NewSession("Default", true))
                    {
                        string script = scriptItemField.GetValue(false);
                        if (!string.IsNullOrEmpty(script))
                        {
                            var results = scriptSession.ExecuteScriptPart(script, false);
                            //if anything in results is a non-false value, return true
                            ruleResponse = results.Any(r => bool.Parse(r.ToString()));
                        }
                        else
                        {
                            Log.Warn("Selected Script Item is empty", this);
                        }
                    }
                }
                else
                {
                    Log.Warn("Selected Item is not a Script", this);
                }
                
            }
            catch (Exception ex)
            {
                Log.Error("Error in Boolean Script Rule", ex, this);
                throw;
            }
            return ruleResponse;
        }
    }
}