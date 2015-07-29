using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Rules;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsDiagnostic.Test, "Rule")]
    [OutputType(typeof (bool))]
    public class TestRuleCommand : BaseCommand
    {
        private static readonly string[] databases = Factory.GetDatabaseNames(); 
        private RuleList<RuleContext> rules;

        [Parameter]
        public string Rule { get; set; }

        [Parameter]
        public PSObject InputObject  { get; set; }

        [ValidateSet("*")]
        [Parameter]
        public string RuleDatabase { get; set; }

        protected override void BeginProcessing()
        {
            Item currentItem = InputObject.BaseObject() as Item;

            string ruleDatabaseName = RuleDatabase;
            if (!string.IsNullOrEmpty(ruleDatabaseName))
            {
                ruleDatabaseName = currentItem != null
                    ? currentItem.Database.Name
                    : ApplicationSettings.RulesDb;
            }

            Database ruleDatabase = Factory.GetDatabase(ruleDatabaseName);
            rules = RuleFactory.ParseRules<RuleContext>(ruleDatabase, Rule);
        }

        protected override void ProcessRecord()
        {
            Item currentItem = InputObject.BaseObject() as Item; 
            
            var ruleContext = new RuleContext
            {
                Item = currentItem
            };

            WriteObject(!rules.Rules.Any() || rules.Rules.Any(rule => rule.Evaluate(ruleContext)));
        }

        public override object GetDynamicParameters()
        {
            if (!_reentrancyLock.WaitOne(0))
            {
                _reentrancyLock.Set();

                SetValidationSetValues("RuleDatabase", databases);

                _reentrancyLock.Reset();
            }

            return base.GetDynamicParameters();
        }
    }
}