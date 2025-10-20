
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Spe.Core.Rules;

public class SitecoreConfigSettingValueCondition<T> : StringOperatorCondition<T> where T : RuleContext
{
    public string Setting { get; set; }
    public string Value { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull((object)ruleContext, nameof(ruleContext));
        var settingValue = Sitecore.Configuration.Settings.GetSetting(Setting ?? string.Empty) ?? string.Empty;
        return Compare(settingValue, Value ?? string.Empty);
    }

}        
