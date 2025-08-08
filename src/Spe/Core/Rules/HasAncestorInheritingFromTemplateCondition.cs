using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Spe.Core.Extensions;

namespace Spe.Core.Rules;

public class HasAncestorInheritingFromTemplateCondition<T> : WhenCondition<T> where T : RuleContext
{
    public ID TemplateId { get; set; }

    protected override bool Execute(T ruleContext)
    {
        Assert.ArgumentNotNull(ruleContext, "ruleContext");
        var item = ruleContext.Item;

        if (item == null || TemplateId == ID.Null || item.Parent == null)
        {
            return false;
        }

        var ancestorOfTemplate = item.Parent.GetAncestorOfTemplate(TemplateId);
        return ancestorOfTemplate != null;
    }
    
}