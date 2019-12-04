using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpePublishManager : IPublishManager
    {
        public IJob PublishAsync(PublishOptions options)
        {
            var publisher = new Publisher(options);
            var job = publisher.PublishAsync();

            return new SpeJob(job);
        }

        public PublishResult PublishSync(PublishOptions options)
        {
            var publishContext = PublishManager.CreatePublishContext(options);
            publishContext.Languages = new[] { options.Language };

            return PublishPipeline.Run(publishContext);
        }
    }
}
