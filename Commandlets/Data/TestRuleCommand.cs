using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Rules;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Data;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsDiagnostic.Test, "Rule")]
    [OutputType(typeof (bool))]
    public class TestRuleCommand : BaseCommand, IDynamicParameters
    {
        private readonly string[] databases = Factory.GetDatabaseNames(); 
        private RuleList<RuleContext> rules;

        [Parameter]
        public string Rule { get; set; }

        [Parameter]
        public PSObject InputObject  { get; set; }


        public TestRuleCommand()
        {
            AddDynamicParameter<string>("RuleDatabase", new ParameterAttribute
            {
                ParameterSetName = ParameterAttribute.AllParameterSets,
                Mandatory = false
            }, new ValidateSetAttribute(databases));
        }

        protected override void BeginProcessing()
        {
            Item currentItem = InputObject.BaseObject() as Item;

            string ruleDatabaseName;
            if (!TryGetParameter("RuleDatabase", out ruleDatabaseName))
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