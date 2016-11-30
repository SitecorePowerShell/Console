using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Layouts;

namespace Cognifide.PowerShell.Commandlets.Presentation
{
    public abstract class BaseLayoutCommand : BaseItemCommand
    {
        [Parameter]
        public SwitchParameter FinalLayout { get; set; }

        protected ID LayoutFieldId { get; set; }

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            LayoutFieldId = Sitecore.FieldIDs.LayoutField;
            if (FinalLayout)
            {
                SitecoreVersion.V80.OrNewer(
                    () => LayoutFieldId = Sitecore.FieldIDs.FinalLayoutField)
                    .ElseWriteWarning(this, nameof(FinalLayout), true);
            }
        }

    }
}