using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Globalization;
using Spe.Core.Extensions;
using Spe.Core.Utility;
using Spe.Core.Validation;

namespace Spe.Commands.Data
{
    [Cmdlet(VerbsCommon.Set, "ItemTemplate", SupportsShouldProcess = true)]
    public class SetItemTemplateCommand : BaseLanguageAgnosticItemCommand
    {
        [Flags]
        public enum FieldCopyOptions
        {
            None = 0,
            SkipStandardValue = 1,
            StopOnFieldError = 2,
            CopyAllFields = 4
        }

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
        public virtual TemplateItem TemplateItem { get; set; }

        [Parameter(ParameterSetName = "Item from Path, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from ID, set by Template", Mandatory = true)]
        [Parameter(ParameterSetName = "Item from Pipeline, set by Template", Mandatory = true)]
        [AutocompleteSet(nameof(Templates))]
        public virtual string Template { get; set; }

        [Parameter]
        public virtual Hashtable FieldsToCopy { get; set; }

        [Parameter]
        public virtual FieldCopyOptions FieldCopyBehavior { get; set; }

        public static string[] Templates => MiscAutocompleteSets.Templates;

        protected override void ProcessItem(Item item)
        {
            if (Template != null)
            {
                // Get template item, and implicitly cast to TemplateItem as this will perform a template-check on the
                // item returned, to ensure it is a template.

                TemplateItem = TemplateUtils.GetFromPath(Template, CurrentDrive);
            }

            // GetFromPath will WriteError if not found, and the cast will throw an error if item is not a template, so no need to handle separately here.

            if (!ShouldProcess(item.GetProviderPath(),
                $"Set item template '{TemplateItem.InnerItem.GetProviderPath()}'")) return;

            var newTemplate = TemplateManager.GetTemplate(TemplateItem.ID, TemplateItem.Database);
            var oldTemplate = TemplateManager.GetTemplate(item.TemplateID, item.Database);
            if (oldTemplate == null)
            {
                WriteVerbose(Translate.Text(Texts.TemplateMissing, item.TemplateID, item.Database));
                oldTemplate = newTemplate;
            }

            var changeList = FieldCopyBehavior.HasFlag(FieldCopyOptions.CopyAllFields) ?
                oldTemplate.GetTemplateChangeList(newTemplate) : new TemplateChangeList(oldTemplate, newTemplate);
            if (FieldsToCopy == null)
            {
                TemplateManager.ChangeTemplate(item, changeList);
                return;
            }

            var stopOnFieldError = FieldCopyBehavior.HasFlag(FieldCopyOptions.StopOnFieldError);
            var values = new Dictionary<string, string>();
            foreach (var fieldName in FieldsToCopy.Keys.OfType<string>())
            {
                var field = item.Fields[fieldName];
                if (field == null)
                {
                    WriteError(typeof(MissingFieldException),
                        $"Source template does not contain '{fieldName}' field.",
                        ErrorIds.FieldNotFound, ErrorCategory.ObjectNotFound, item);
                    if (stopOnFieldError) return;
                    continue;
                }

                var targetFieldName = FieldsToCopy[fieldName].ToString();
                if (stopOnFieldError && newTemplate.GetField(targetFieldName) == null)
                {
                    WriteError(typeof(MissingFieldException),
                        $"Target template does not contain '{targetFieldName}' field.",
                        ErrorIds.FieldNotFound, ErrorCategory.ObjectNotFound, item);
                    return;
                }

                if (!FieldCopyBehavior.HasFlag(FieldCopyOptions.SkipStandardValue) || 
                    !field.ContainsStandardValue)
                {
                    values.Add(fieldName, field.Value);
                }
            }

            TemplateManager.ChangeTemplate(item, changeList);

            item.Edit(args =>
            {
                foreach (var fieldName in values.Keys)
                {
                    var field = item.Fields[FieldsToCopy[fieldName].ToString()];
                    if(field == null)
                    {
                        WriteError(typeof(MissingFieldException),
                            $"Target template does not contain '{fieldName}' field.",
                            ErrorIds.FieldNotFound, ErrorCategory.ObjectNotFound, item);
                        //At this point we should keep going. The template has already been changed.
                        continue;
                    }
                    
                    field.Value = values[fieldName];
                }
            });
        }
    }
}