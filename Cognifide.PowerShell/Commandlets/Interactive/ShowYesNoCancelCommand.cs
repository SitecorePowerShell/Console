using System.Management.Automation;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "YesNoCancel")]
    [OutputType(typeof (string))]
    public class ShowYesNoCancelCommand : BaseFormCommand
    {
        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction())
                {
                    WriteObject("error");
                    return;
                }

                var jobUiManager = TypeResolver.Resolve<IJobUiManager>();
                var yesnoresult = jobUiManager.ShowModalDialog(Title, "YesNoCancel", WidthString, HeightString);
                WriteObject(yesnoresult);
            });
        }
    }
}