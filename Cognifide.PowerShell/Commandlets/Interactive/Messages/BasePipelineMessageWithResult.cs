using System;
using System.Web;
using Cognifide.PowerShell.Client.Applications;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public abstract class BasePipelineMessageWithResult : IMessageWithResult
    {
        private Handle jobHandle;
        [NonSerialized] private readonly MessageQueue messageQueue;

        protected Handle JobHandle => jobHandle;

        protected virtual bool WaitForPostBack => true;

        public virtual object GetResult()
        {
            return JobContext.IsJob ? JobContext.MessageQueue.GetResult() : MessageQueue.GetResult();
        }

        private MessageQueue MessageQueue => messageQueue;

        protected BasePipelineMessageWithResult()
        {
            messageQueue = new MessageQueue();
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }
        }

        /// <summary>
        ///     Starts the pipeline.
        /// </summary>
        public void Execute()
        {
            Context.ClientPage.Start(this, "Pipeline");
        }

        protected abstract void ShowUI();

        protected virtual object ProcessResult(bool hasResult, string result)
        {
            return hasResult ? result : null;
        }

        private bool MonitorActive
        {
            set
            {
                var runner = Context.ClientPage.CodeBeside as IPowerShellRunner;
                if (runner != null)
                {
                    runner.MonitorActive = value;
                }
            }
        }

        /// <summary>
        ///     Entry point for a pipeline.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Pipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                if (jobHandle != null)
                {
                    Context.ClientPage.ServerProperties["#pipelineJob"] = jobHandle.ToString();
                }

                MonitorActive = false;
                ShowUI();

                if (WaitForPostBack)
                {
                    args.WaitForPostBack();
                }
                else
                {
                    MonitorActive = true;
                }
            }
            else
            {
                var processedResult = ProcessResult(args.HasResult, args.Result);

                var strJobId = StringUtil.GetString(Context.ClientPage.ServerProperties["#pipelineJob"]);
                if (!String.IsNullOrEmpty(strJobId))
                {
                    jobHandle = Handle.Parse(strJobId);
                    var job = JobManager.GetJob(jobHandle);
                    job?.MessageQueue.PutResult(processedResult);
                }
                else
                {
                    MessageQueue.PutResult(processedResult);
                }

                MonitorActive = true;
            }
        }
    }
}