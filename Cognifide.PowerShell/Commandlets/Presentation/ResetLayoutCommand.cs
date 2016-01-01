using System;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;
using Cognifide.PowerShell.Commandlets.Security.Items;

namespace Cognifide.PowerShell.Commandlets.Presentation
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