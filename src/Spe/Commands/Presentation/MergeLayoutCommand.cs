using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsData.Merge, "Layout", SupportsShouldProcess = true)]
    public class MergeLayoutCommand : BaseItemCommand
    {

        protected override void ProcessItem(Item item)
        {
            if (!ShouldProcess(item.GetProviderPath(),
                $"Merging layout from '{item.Language.Name}' to shared layout.")) return;

            var shared = new LayoutField(item.Fields[Sitecore.FieldIDs.LayoutField]);

            var final = new LayoutField(item.Fields[Sitecore.FieldIDs.FinalLayoutField]);

            //If we don't have a final layout delta, we're good!
            if (string.IsNullOrWhiteSpace(final.Value))
            {
                WriteVerbose("No final layout - nothing to do.");
                return;
            }

            var finalLayoutDefinition = LayoutDefinition.Parse(final.Value);

            item.Edit(p =>
            {
                LayoutField.SetFieldValue(item.Fields[Sitecore.FieldIDs.LayoutField],
                    finalLayoutDefinition.ToXml());
                shared.Value = finalLayoutDefinition.ToXml();
                final.InnerField.Reset();
            });
        }
    }
}