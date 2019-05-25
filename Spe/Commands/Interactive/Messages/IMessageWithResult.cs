using Sitecore.Jobs.AsyncUI;

namespace Spe.Commands.Interactive.Messages
{
    public interface IMessageWithResult : IMessage
    {
        object GetResult();

    }
}