using System.Management.Automation;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsData.Update, "ListView")]
    [OutputType(new[] {typeof (string)})]
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