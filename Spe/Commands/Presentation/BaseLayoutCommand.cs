using System.Management.Automation;
using Sitecore.Data;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Presentation
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