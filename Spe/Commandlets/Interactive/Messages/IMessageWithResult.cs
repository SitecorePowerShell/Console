using Sitecore.Jobs.AsyncUI;

namespace Spe.Commandlets.Interactive.Messages
{
    public interface IMessageWithResult : IMessage
    {
        object GetResult();

    }
}