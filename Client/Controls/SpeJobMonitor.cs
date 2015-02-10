using System;
using System.Threading;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Sites;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class SpeJobMonitor : Control
    {
        /// <summary>
        /// Gets or sets the task.
        /// 
        /// </summary>
        /// 
        /// <value>
        /// The task.
        /// </value>
        public Handle JobHandle
        {
            get
            {
                string viewStateString = this.GetViewStateString("task");
                if (!string.IsNullOrEmpty(viewStateString))
                    return Handle.Parse(viewStateString);
                else
                    return Handle.Null;
            }
            set
            {
                this.SetViewStateString("task", value.ToString());
            }
        }

        public bool Active
        {
            get
            {
                return GetViewStateBool("active",true);
            }
            set
            {
                SetViewStateBool("active", value);
                if (value)
                {
                    ScheduleCallback();
                }
            }
        }

        /// <summary>
        /// Occurs when the task is finished.
        /// </summary>
        public event EventHandler JobFinished;

        /// <summary>
        /// Occurs when check message received without current task.
        /// 
        /// </summary>
        public event EventHandler JobDisappeared;

        /// <summary>
        /// Handles the message.
        /// 
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            if (message.Name != "pstaskmonitor:check")
                return;
            Handle jobHandle = this.JobHandle;
            if (jobHandle == Handle.Null)
                return;
            if (!jobHandle.IsLocal)
            {
                ScheduleCallback();
            }
            else
            {
                Job job = JobManager.GetJob(jobHandle);
                if (job == null)
                {
                    this.OnJobDisappeared();
                }
                else
                {
                    IMessage iMessage;
                    while (job.MessageQueue.GetMessage(out iMessage))
                    {
                        iMessage.Execute();
                        if (iMessage is CompleteMessage)
                        {
                            this.OnJobFinished();
                            return;
                        }
                    }
                    ScheduleCallback();
                }
            }
        }

        private void OnJobDisappeared()
        {
            this.JobHandle = Handle.Null;
            if (this.JobDisappeared == null)
                return;
            this.JobDisappeared((object)this, EventArgs.Empty);
        }

        /// <summary>
        /// Stops monitoring the job and fires up <see cref="E:Sitecore.Jobs.AsyncUI.JobMonitor.JobFinished"/> event.
        /// 
        /// </summary>
        private void OnJobFinished()
        {
            this.JobHandle = Handle.Null;
            if (this.JobFinished == null)
                return;
            this.JobFinished((object)this, EventArgs.Empty);
        }

        /// <summary>
        /// Starts the specified task.
        /// 
        /// </summary>
        /// <param name="name">The name.</param><param name="category">The category.</param><param name="task">The entry.</param>
        public void Start(string name, string category, ThreadStart task)
        {
            Assert.ArgumentNotNullOrEmpty(name, "name");
            Assert.ArgumentNotNullOrEmpty(category, "category");
            Assert.ArgumentNotNull((object)task, "task");
            string siteName = string.Empty;
            SiteContext site = Sitecore.Context.Site;
            if (site != null)
                siteName = site.Name;
            this.JobHandle = JobManager.Start(new JobOptions(name, category, siteName, (object)new SpeJobMonitor.TaskRunner(task), "Run")
            {
                ContextUser = Sitecore.Context.User,
                AtomicExecution = false
            }).Handle;
            ScheduleCallback();
        }

        /// <summary>
        /// Schedules the callback.
        /// 
        /// </summary>
        private void ScheduleCallback()
        {
            if (Active)
            {
                SheerResponse.Timer("pstaskmonitor:check", 500);
            }
        }

        /// <summary>
        /// Adaptor from ThreadStart to job API
        /// 
        /// </summary>
        public class TaskRunner
        {
            /// <summary>
            /// Task to execute
            /// 
            /// </summary>
            private ThreadStart _task;

            /// <summary>
            /// Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.JobMonitor.TaskRunner"/> class.
            /// 
            /// </summary>
            /// <param name="task">The task.</param>
            public TaskRunner(ThreadStart task)
            {
                Assert.ArgumentNotNull((object)task, "task");
                this._task = task;
            }

            /// <summary>
            /// Runs the task inside the job.
            /// 
            /// </summary>
            public void Run()
            {
                this._task();
                JobContext.MessageQueue.PutMessage(new CompleteMessage());               
                JobContext.MessageQueue.GetResult();
            }
        }

    }
}