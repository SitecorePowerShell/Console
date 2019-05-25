using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;

namespace Spe.Commandlets.Data
{
    [Cmdlet(VerbsDiagnostic.Test, "BaseTemplate")]
    public class TestBaseTemplateCommand : BaseTemplateItemCommand
    {
        protected override void ProcessTemplateItem(TemplateItem templateItem)
        {
            var inherits = true;
            var testedTemplate = TemplateManager.GetTemplate(templateItem.ID, templateItem.Database);
            foreach (var template in TemplateItem)
            {
                if (!testedTemplate.InheritsFrom(template.ID))
                {
                    inherits = false;
                }
            }
            WriteObject(inherits);
        }
    }
}