using System.Management.Automation;

namespace Spe.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Close, "Window")]
    public class CloseWindowCommand : BaseShellCommand
    {
        protected override void ProcessRecord()
        {
            HostData.CloseRunner = true;
        }
    }
}