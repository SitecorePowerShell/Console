using System;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    public abstract class BaseTemplateItemCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, set by TemplateItem",
            Mandatory = true)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, set by Template", Mandatory = true)
        ]
        public override Item Item { get; set; }

        [Parameter(ParameterSetName = "Item from Path, set by TemplateItem", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Path, set by Template", Mandatory = true)]
        [Alias("FullName", "FileName")]
        public override string Path { get; set; }

        [Parameter(ParameterSetName = "Item from ID, set by TemplateItem", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, set by Template", Mandatory = true)]
        public override string Id { get; set; }

        [Parameter(ParameterSetName = "Item from Path, set by TemplateItem", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, set by TemplateItem", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline, set by TemplateItem", Mandatory = true)]
        public virtual TemplateItem[] TemplateItem { get; set; }

        [Parameter(ParameterSetName = "Item from Path, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline, set by Template", Mandatory = true)]
        [AutocompleteSet("Templates")]
        public virtual string[] Template { get; set; }

        public static string[] Templates => MiscAutocompleteSets.Templates;

        protected override void ProcessItem(Item item)
        {

            if (item.TemplateID != TemplateIDs.Template)
            {
                item = item.Database.GetTemplate(item.TemplateID);
            }

            if (Template != null)
            {
                // Get template item and implicitly cast to TemplateItem as this will perform a template-check on the
                // item returned, to ensure it is a template.

                TemplateItem = Template.Select(templateName => (TemplateItem)TemplateUtils.GetFromPath(templateName, item.Database.Name)).ToArray();
            }

            ProcessTemplateItem(item);
        }

        protected abstract void ProcessTemplateItem(TemplateItem item);

    }
}