
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Spe.Core.Rules;

public class SitecoreConfigIntSettingValueCondition<T> : IntegerComparisonCondition<T> where T : RuleContext
{
    public string Setting { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull((object)ruleContext, nameof(ruleContext));
        var settingValue = Sitecore.Configuration.Settings.GetIntSetting(Setting ?? string.Empty,0);
        return this.Compare(settingValue);
    }

}        
