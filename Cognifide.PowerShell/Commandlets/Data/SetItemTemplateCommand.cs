using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
using Sitecore.Exceptions;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Set, "ItemTemplate", SupportsShouldProcess = true)]
    public class SetItemTemplateCommand : BaseLanguageAgnosticItemCommand
    {
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, set by TemplateItem", Mandatory = true)]
        [Parameter(ValueFromPipeline = true, ParameterSetName = "Item from Pipeline, set by Template", Mandatory = true)]
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
        public virtual TemplateItem TemplateItem { get; set; }

        [Parameter(ParameterSetName = "Item from Path, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline, set by Template", Mandatory = true)]
        public virtual string Template { get; set; }

        protected override void ProcessItem(Item item)
        {
            if (Template != null)
            {
                // Get template item, and implicitly cast to TemplateItem as this will perform a template-check on the
                // item returned, to ensure it is a template.

                TemplateItem = TemplateUtils.GetFromPath(Template, CurrentDrive);
            }

            // GetFromPath will WriteError if not found, and the cast will throw an error if item is not a tempalte, so no need to handle separately here.

            if (ShouldProcess(item.GetProviderPath(), string.Format("Set item template '{0}'", TemplateItem.InnerItem.GetProviderPath())))
            {
                item.ChangeTemplate(TemplateItem);
            }
        }
    }
}