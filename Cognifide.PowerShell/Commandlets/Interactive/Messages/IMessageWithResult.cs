using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    public interface IMessageWithResult : IMessage
    {
        object GetResult();

    }
}