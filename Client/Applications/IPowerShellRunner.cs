using Cognifide.PowerShell.Client.Controls;

namespace Cognifide.PowerShell.Client.Applications
{
    public interface IPowerShellRunner
    {
        SpeJobMonitor Monitor { get; }
    }
}