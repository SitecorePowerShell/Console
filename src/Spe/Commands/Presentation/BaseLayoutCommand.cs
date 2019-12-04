using System.Management.Automation;
using Sitecore.Data;

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
            if (!FinalLayout) return;

            LayoutFieldId = Sitecore.FieldIDs.FinalLayoutField;
        }
    }
}