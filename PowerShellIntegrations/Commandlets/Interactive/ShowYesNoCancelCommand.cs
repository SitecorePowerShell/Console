using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "YesNoCancel", SupportsShouldProcess = true, DefaultParameterSetName = "Name")]
    public class ShowYesNoCancelCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            LogErrors(() =>
                {
                    string yesnoresult = JobContext.ShowModalDialog(Title, "YesNoCancel", WidthString, HeightString);
                    WriteObject(yesnoresult);
                });
        }
    }
}