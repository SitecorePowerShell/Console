using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Cognifide.PowerShell.Core.Utility;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Reset, "ItemField", SupportsShouldProcess = true)]
    public class ResetItemFieldCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter IncludeStandardFields { get; set; }

        [Parameter]
        [Alias("FieldName")]
        public string[] Name { get; set; }

        protected List<WildcardPattern> NameWildcardPatterns { get; private set; }

        protected override void BeginProcessing()
        {
            Language = null;
            base.BeginProcessing();

            if (Name != null && Name.Any())
            {
                NameWildcardPatterns =
                    Name.Select(
                        name =>
                            new WildcardPattern(name, WildcardOptions.IgnoreCase | WildcardOptions.CultureInvariant))
                        .ToList();
            }
        }

        protected override void ProcessItem(Item item)
        {
            var matchingFields = GetMatchingFields(item).ToList();

            if (matchingFields.Any())
            {
                if (ShouldProcess(item.GetProviderPath(), string.Format("Reset field(s) '{0}'",
                    matchingFields.Select(r => r.DisplayName).Aggregate((seed, curr) => seed + ", " + curr))))
                {
                    item.Editing.BeginEdit();

                    foreach (var field in matchingFields)
                    {
                        field.Reset();
                    }

                    item.Editing.EndEdit();
                }
            }
        }

        protected IEnumerable<Field> GetMatchingFields(Item item)
        {
            item.Fields.ReadAll();

            var template =
                TemplateManager.GetTemplate(Settings.DefaultBaseTemplate,
                    item.Database);

            foreach (Field field in item.Fields)
            {
                if (NameWildcardPatterns != null &&
                    !NameWildcardPatterns.Any(nameWildcardPattern => nameWildcardPattern.IsMatch(field.Name)))
                {
                    continue;
                }
                if (!IncludeStandardFields && template.ContainsField(field.ID))
                {
                    continue;
                }

                yield return field;
            }
        }
    }
}