using System.Management.Automation;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "YesNoCancel")]
    [OutputType(new[] {typeof (string)})]
    public class ShowYesNoCancelCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            AssertDefaultSize(500, 100);
            LogErrors(() =>
            {
                string yesnoresult = JobContext.ShowModalDialog(Title, "YesNoCancel", WidthString, HeightString);
                WriteObject(yesnoresult);
            });
        }
    }
}