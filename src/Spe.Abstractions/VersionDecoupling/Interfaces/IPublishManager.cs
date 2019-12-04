using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IPublishManager
    {
        IJob PublishAsync(PublishOptions options);

        PublishResult PublishSync(PublishOptions options);
    }
}