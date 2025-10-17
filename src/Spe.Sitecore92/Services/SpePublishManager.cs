using Sitecore.Jobs;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.Publish;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using System.Reflection;
using System;
using System.Linq;
using System.Threading;
using Sitecore;

namespace Spe.VersionSpecific.Services
{
    public class SpePublishManager : IPublishManager
    {
        private const int PollIntervalMs = 500;

        private static readonly bool HasAbortedState = Enum.GetNames(typeof(JobState)).Any(n => n == "Aborted");

        private static readonly PropertyInfo StatusStatisticsProperty =
            typeof(PublishStatus).GetProperty("Statistics", BindingFlags.Public | BindingFlags.Instance);

        private static readonly bool HasStatusStatistics = StatusStatisticsProperty != null;

        private static readonly PropertyInfo PublishResultStatisticsProperty =
            typeof(PublishResult).GetProperty("Statistics", BindingFlags.Public | BindingFlags.Instance);

        private static bool IsAborted(JobState state) => HasAbortedState && string.Equals(state.ToString(), "Aborted", StringComparison.Ordinal);

        private static bool IsFinished(JobState state) => string.Equals(state.ToString(), "Finished", StringComparison.Ordinal);

        public IJob PublishAsync(PublishOptions options)
        {
            var publisher = new Publisher(options);
            var job = publisher.PublishAsync();

            return new SpeJob(job);
        }

        public PublishResult PublishSync(PublishOptions options)
        {
            if (HasStatusStatistics)
            {
                var handle = PublishManager.Publish(new[] { options });
                var status = WaitForCompletion(handle);
                return BuildPublishResult(status);
            }
            else
            {
                var publishContext = PublishManager.CreatePublishContext(options);
                publishContext.Languages = new[] { options.Language };
                publishContext.Job = Sitecore.Context.Job;

                return PublishPipeline.Run(publishContext);
            }
        }

        private static PublishStatus WaitForCompletion(Handle handle)
        {
            var status = PublishManager.GetStatus(handle);
            while (status != null && !IsFinished(status.State) && !IsAborted(status.State))
            {
                Thread.Sleep(PollIntervalMs);
                status = PublishManager.GetStatus(handle);
            }
            return status;
        }

        private static PublishResult BuildPublishResult(PublishStatus status)
        {
            var result = new PublishResult();
            if (status != null &&
                StatusStatisticsProperty != null &&
                PublishResultStatisticsProperty != null &&
                PublishResultStatisticsProperty.CanWrite)
            {
                var statsValue = StatusStatisticsProperty.GetValue(status);
                PublishResultStatisticsProperty.SetValue(result, statsValue);
            }
            return result;
        }
    }
}
