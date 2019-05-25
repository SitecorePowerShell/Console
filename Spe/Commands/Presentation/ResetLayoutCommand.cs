using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Spe.Core.Extensions;
using Spe.Core.Utility;

namespace Spe.Commands.Presentation
{
    [Cmdlet(VerbsCommon.Reset, "Layout", SupportsShouldProcess = true)]
    public class ResetLayoutCommand : BaseLayoutCommand
    {
        protected override void ProcessItem(Item item)
        {
            if (ShouldProcess(item.GetProviderPath(), "Reset layout"))
            {
                Field layoutField = item.Fields[LayoutFieldId];

                if (layoutField == null)
                {
                    return;
                }

                item.Edit(p =>
                {
                    layoutField.Reset();
                });
            }
        }
    }
}