using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Rules;
using Spe.Core.Extensions;
using Spe.Core.Settings;
using Spe.Core.Validation;

namespace Spe.Commands.Data
{
    [Cmdlet(VerbsDiagnostic.Test, "Rule")]
    [OutputType(typeof (bool))]
    public class TestRuleCommand : BaseCommand
    {
        private RuleList<RuleContext> rules;

        [Parameter]
        public string Rule { get; set; }

        [Parameter]
        public PSObject InputObject  { get; set; }

        [AutocompleteSet(nameof(Databases))]
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
    }
}