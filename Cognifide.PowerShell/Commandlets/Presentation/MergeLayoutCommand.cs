using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Sitecore.Shell.Framework.Commands.Ribbon;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    [Cmdlet(VerbsData.Merge, "Layout", SupportsShouldProcess = true)]
    public class MergeLayoutCommand : BaseItemCommand
    {

        protected override void ProcessItem(Item item)
        {
            SitecoreVersion.V80.OrNewer(
                () =>
                {

                    if (ShouldProcess(item.GetProviderPath(),
                        $"Merging layout from '{item.Language.Name}' to shared layout."))
                    {

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
                }).ElseWriteWarning(this, "Merge-Layout", false);

        }
    }
}