using System.Management.Automation;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsData.Update, "ListView")]
    [OutputType(typeof (string))]
    public class UpdateListViewCommand : BaseListViewCommand
    {
        public override string Title { get; set; }
        public override int Width { get; set; }
        public override int Height { get; set; }

        protected override void EndProcessing()
        {
            LogErrors(() => SessionState.PSVariable.Set("allDataInternal", cumulativeData));
            base.EndProcessing();
        }
    }
}