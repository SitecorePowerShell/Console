﻿using System;
using System.Threading;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Client.Applications;
using Spe.Core.VersionDecoupling;

namespace Spe.Client.Controls
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
                return !string.IsNullOrEmpty(viewStateString) ? Handle.Parse(viewStateString) : Handle.Null;
            }
            set { SetViewStateString("task", value.ToString()); }
        }

        public string SessionID
        {
            get
            {
                var viewStateString = GetViewStateString("taskMonitorSessionId");
                return viewStateString;
            }
            set
            {
                SetViewStateString("taskMonitorSessionId", value);
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
            if (jobHandle.Equals((object)Handle.Null))
                return;

            if (!jobHandle.IsLocal)
            {
                ScheduleCallback();
            }
            else
            {
                var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
                var job = jobManager.GetJob(jobHandle);
                if (job == null)
                {
                    OnJobDisappeared();
                }
                else
                {
                    while (job.MessageQueue.GetMessage(out IMessage iMessage))
                    {
                        iMessage.Execute();
                        if (iMessage is CompleteMessage completeMessage)
                        {
                            OnJobFinished(completeMessage.RunnerOutput);
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
            JobDisappeared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Stops monitoring the job and fires up <see cref="E:Sitecore.Jobs.AsyncUI.JobMonitor.JobFinished" /> event.
        /// </summary>
        private void OnJobFinished(RunnerOutput runnerOutput)
        {
            JobHandle = Handle.Null;
            var eventArgs = new SessionCompleteEventArgs { RunnerOutput = runnerOutput };
            JobFinished?.Invoke(this, eventArgs);
        }

        public void Start(string name, string category, ThreadStart task, Language language = null, User user = null,
            IJobOptions options = null)
        {
            Assert.ArgumentNotNullOrEmpty(name, "name");
            Assert.ArgumentNotNullOrEmpty(category, "category");
            Assert.ArgumentNotNull(task, "task");

            var siteName = Sitecore.Context.Site?.Name ?? string.Empty;
            var currentUser = user ?? options?.ContextUser ?? Sitecore.Context.User;
            var jobOptions = TypeResolver.Resolve<IJobOptions>(new object[] { $"{name} - {currentUser?.Name}", category, siteName, new TaskRunner(task), "Run" });
            jobOptions.ContextUser = currentUser;
            jobOptions.AtomicExecution = false;
            jobOptions.EnableSecurity = options?.EnableSecurity ?? true;
            jobOptions.ClientLanguage = language ?? options?.ClientLanguage ?? Sitecore.Context.Language;
            jobOptions.AfterLife = new TimeSpan(0, 0, 0, 10);

            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            JobHandle = jobManager.StartJob(jobOptions);
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
            private readonly ThreadStart task;

            /// <summary>
            ///     Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.JobMonitor.TaskRunner" /> class.
            /// </summary>
            /// <param name="task">The task.</param>
            public TaskRunner(ThreadStart task)
            {
                Assert.ArgumentNotNull(task, "task");
                this.task = task;
            }

            /// <summary>
            ///     Runs the task inside the job.
            /// </summary>
            public void Run()
            {
                task();
            }
        }
    }
}