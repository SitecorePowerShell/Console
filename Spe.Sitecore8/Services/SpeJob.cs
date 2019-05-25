using System.Collections.Specialized;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeJob : IJob
    {
        public Handle Handle => Job.Handle;

        internal Job Job { get; set; }

        public MessageQueue MessageQueue => Job.MessageQueue;

        public string Name => Job.Name;

        public bool StatusFailed
        {
            get => Job.Status.Failed;
            set => Job.Status.Failed = value;
        }

        public object StatusResult
        {
            get => Job.Status.Result;
            set => Job.Status.Result = value;
        }

        public StringCollection StatusMessages => Job.Status.Messages;

        public IJobOptions Options => (SpeJobOptions) Job.Options;

        public bool IsDone => Job.IsDone;

        public SpeJob(Handle handle)
        {
            Job = JobManager.GetJob(handle);
        }

        public SpeJob(Job job)
        {
            Job = job;
        }

        public void AddStatusMessage(string message)
        {
            Job.Status.Messages.Add(message);
        }

        public static implicit operator Job(SpeJob job)
        {
            return job.Job;
        }
    }
}
