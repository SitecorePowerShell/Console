using System.Web.Configuration;

using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Spe.Core.Rules;

public class WebConfigSettingValueCondition<T> : StringOperatorCondition<T> where T : RuleContext
{
    public string Setting { get; set; }
    public string Value { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull((object)ruleContext, nameof(ruleContext));
        string str = Value ?? string.Empty;
        var currentEnvironment = WebConfigurationManager.AppSettings[Setting] ?? string.Empty;
        return Compare(currentEnvironment, str);
    }

}        
