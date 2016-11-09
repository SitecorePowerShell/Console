using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Exceptions;
using Sitecore.Pipelines.Save;

namespace Cognifide.PowerShell.Commandlets.Data
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