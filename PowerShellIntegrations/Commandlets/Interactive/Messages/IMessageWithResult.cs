using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    public interface IMessageWithResult
    {
        MessageQueue MessageQueue { get; }
    }
}