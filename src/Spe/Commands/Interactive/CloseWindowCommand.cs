using System.Management.Automation;

namespace Spe.Commands.Interactive
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