using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "ItemTemplate")]
    [OutputType(typeof (TemplateItem))]
    public class GetItemTemplateCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter]
        public SwitchParameter Recurse { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (Recurse)
            {
                var templates = new Dictionary<string, TemplateItem>();
                var template = item.Template;
                GetBaseTemplates(template, templates);
                WriteObject(templates.Values, true);
            }
            else
            {
                WriteObject(item.Template, true);
            }
        }

        private static void GetBaseTemplates(TemplateItem template, IDictionary<string, TemplateItem> templates)
        {
            if (template != null && !templates.ContainsKey(template.FullName))
            {
                templates.Add(template.FullName, template);
                foreach (var baseTemplate in template.BaseTemplates)
                {
                    GetBaseTemplates(baseTemplate, templates);
                }
            }
        }
    }
}