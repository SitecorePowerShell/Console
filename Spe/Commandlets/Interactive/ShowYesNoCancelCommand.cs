using System.Management.Automation;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.VersionDecoupling;

namespace Spe.Commandlets.Interactive
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