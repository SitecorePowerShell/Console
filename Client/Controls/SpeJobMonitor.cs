using System;
using System.Threading;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Controls
{
    public class SpeJobMonitor : Control
    {
        /// <summary>
        ///     Gets or sets the task.
        /// </summary>
        /// <value>
        ///     The task.
        /// </value>
        public Handle JobHandle
        {
            get
            {
                var viewStateString = GetViewStateString("task");
                if (!string.IsNullOrEmpty(viewStateString))
                    return Handle.Parse(viewStateString);
                return Handle.Null;
            }
            set { SetViewStateString("task", value.ToString()); }
        }

        public string SessionID
        {
            get
            {
                var sessionId = HttpContext.Current.Session[JobHandle.ToString()];
                if (sessionId != null)
                {
                    return sessionId.ToString();
                }
                return string.Empty;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    HttpContext.Current.Session.Remove(JobHandle.ToString());
                }
                else
                {
                    HttpContext.Current.Session[JobHandle.ToString()] = value;
                }
            }
        }

        public bool Active
        {
            get { return GetViewStateBool("active", true); }
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
        ///     Occurs when the task is finished.
        /// </summary>
        public event EventHandler JobFinished;

        /// <summary>
        ///     Occurs when check message received without current task.
        /// </summary>
        public event EventHandler JobDisappeared;

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            if (message.Name != "pstaskmonitor:check")
                return;
            var jobHandle = JobHandle;
            if (jobHandle == Handle.Null)
                return;
            if (!jobHandle.IsLocal)
            {
                ScheduleCallback();
            }
            else
            {
                var job = JobManager.GetJob(jobHandle);
                if (job == null)
                {
                    OnJobDisappeared();
                }
                else
                {
                    IMessage iMessage;
                    while (job.MessageQueue.GetMessage(out iMessage))
                    {
                        iMessage.Execute();
                        if (iMessage is CompleteMessage)
                        {
                            OnJobFinished();
                            return;
                        }
                    }
                    ScheduleCallback();
                }
            }
        }

        private void OnJobDisappeared()
        {
            JobHandle = Handle.Null;
            if (JobDisappeared == null)
                return;
            JobDisappeared(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Stops monitoring the job and fires up <see cref="E:Sitecore.Jobs.AsyncUI.JobMonitor.JobFinished" /> event.
        /// </summary>
        private void OnJobFinished()
        {
            JobHandle = Handle.Null;
            if (JobFinished == null)
                return;
            JobFinished(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Starts the specified task.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="category">The category.</param>
        /// <param name="task">The entry.</param>
        public void Start(string name, string category, ThreadStart task)
        {
            Assert.ArgumentNotNullOrEmpty(name, "name");
            Assert.ArgumentNotNullOrEmpty(category, "category");
            Assert.ArgumentNotNull(task, "task");
            var siteName = string.Empty;
            var site = Sitecore.Context.Site;
            if (site != null)
                siteName = site.Name;
            JobHandle = JobManager.Start(new JobOptions(name, category, siteName, new TaskRunner(task), "Run")
            {
                ContextUser = Sitecore.Context.User,
                AtomicExecution = false,
                EnableSecurity = true,
                ClientLanguage = Sitecore.Context.ContentLanguage
            }).Handle;
            ScheduleCallback();
        }

        /// <summary>
        ///     Schedules the callback.
        /// </summary>
        private void ScheduleCallback()
        {
            if (Active)
            {
                SheerResponse.Timer("pstaskmonitor:check", 500);
            }
        }

        /// <summary>
        ///     Adaptor from ThreadStart to job API
        /// </summary>
        public class TaskRunner
        {
            /// <summary>
            ///     Task to execute
            /// </summary>
            private readonly ThreadStart _task;

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.JobMonitor.TaskRunner" /> class.
            /// </summary>
            /// <param name="task">The task.</param>
            public TaskRunner(ThreadStart task)
            {
                Assert.ArgumentNotNull(task, "task");
                _task = task;
            }

            /// <summary>
            ///     Runs the task inside the job.
            /// </summary>
            public void Run()
            {
                _task();
                JobContext.MessageQueue.PutMessage(new CompleteMessage());
                JobContext.MessageQueue.GetResult();
            }
        }
    }
}