using Cognifide.PowerShell.SitecoreIntegrations.Controls;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public interface IPowerShellRunner
    {
        SpeJobMonitor Monitor { get; }
    }
}