
using System.Web.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Spe.Core.Rules;

public class WebConfigIntSettingValueCondition<T> : IntegerComparisonCondition<T> where T : RuleContext
{
    public string Setting { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull((object)ruleContext, nameof(ruleContext));
        var settingStringValue = WebConfigurationManager.AppSettings[Setting] ?? string.Empty;
        return int.TryParse(settingStringValue, out var settingValue) && Compare(settingValue);
    }

}        
