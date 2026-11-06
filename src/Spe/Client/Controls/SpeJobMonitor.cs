using System;
using System.Collections.Specialized;
using System.Threading;
using System.Web;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Client.Applications;
using Spe.Core.Host;
using Spe.Core.VersionDecoupling;

namespace Spe.Client.Controls
{
    public class SpeJobMonitor : Control
    {
        private const string CheckMessage = "pstaskmonitor:check";
        private const string HeartbeatMessage = "pstaskmonitor:heartbeat";
        private const int PollDelay = 500;

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
                // The registry stores the full session key so a page that
                // comes back on a new ASP.NET session can still resolve it.
                var session = string.IsNullOrEmpty(value) ? null : ScriptSessionManager.GetSessionIfExists(value);
                if (session != null && !JobHandle.Equals((object)Handle.Null))
                {
                    InteractiveJobRegistry.SetSessionKey(JobHandle.ToString(), session.Key);
                }
            }
        }

        /// <summary>
        ///     Lease this page holds on the job in InteractiveJobRegistry. Empty
        ///     for jobs that are not registered for recovery.
        /// </summary>
        public string LeaseToken
        {
            get => GetViewStateString("leaseToken");
            private set => SetViewStateString("leaseToken", value);
        }

        // Sequence number of the one poll the server will act on. A check with
        // any other number (a throttled timer that fires after the watchdog
        // already revived the chain) is answered with a re-arm only, so a
        // monitor never runs more than one chain.
        private int ExpectedSeq
        {
            get => GetViewStateInt("seq");
            set => SetViewStateInt("seq", value);
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
                else if (!JobHandle.Equals((object)Handle.Null))
                {
                    HoldWatchdog();
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
        ///     Occurs when another page took over the job's lease.
        /// </summary>
        public event EventHandler LeaseLost;

        /// <summary>
        ///     Handles the message.
        /// </summary>
        /// <param name="message">The message.</param>
        public override void HandleMessage(Message message)
        {
            base.HandleMessage(message);
            var isCheck = message.Name == CheckMessage;
            var isHeartbeat = message.Name == HeartbeatMessage;
            if (!isCheck && !isHeartbeat)
                return;

            // Every monitor on the page sees every check; act only on our own.
            if (!string.Equals(message.Arguments["id"], ID, StringComparison.Ordinal))
                return;

            var jobHandle = JobHandle;
            if (jobHandle.Equals((object)Handle.Null))
            {
                DisarmWatchdog();
                return;
            }

            if (!jobHandle.IsLocal)
            {
                if (isCheck) ScheduleCallback();
                return;
            }

            var handle = jobHandle.ToString();
            if (InteractiveJobRegistry.Touch(handle, LeaseToken) == LeaseState.Lost)
            {
                OnLeaseLost();
                return;
            }

            if (isHeartbeat)
                return;

            if (!int.TryParse(message.Arguments["seq"], out var seq) || seq != ExpectedSeq)
            {
                if (Active) ArmWatchdog();
                else HoldWatchdog();
                return;
            }

            ExpectedSeq = seq + 1;

            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetJob(jobHandle) ?? InteractiveJobRegistry.Get(handle)?.Job;
            if (job == null)
            {
                OnJobDisappeared();
                return;
            }

            // Read before draining: everything a finished job queued is already
            // in the queue, so a done job that yields no CompleteMessage (thread
            // abort) has nothing more coming. The registry keeps such a job
            // object alive, so without this it would be polled forever.
            var jobDone = job.IsDone;
            while (job.MessageQueue.GetMessage(out IMessage iMessage))
            {
                iMessage.Execute();
                if (iMessage is CompleteMessage completeMessage)
                {
                    InteractiveJobRegistry.Remove(handle);
                    OnJobFinished(completeMessage.RunnerOutput);
                    return;
                }
            }

            if (jobDone)
            {
                OnJobDisappeared();
                return;
            }

            ScheduleCallback();
        }

        private void OnJobDisappeared()
        {
            InteractiveJobRegistry.Remove(JobHandle.ToString());
            JobHandle = Handle.Null;
            LeaseToken = string.Empty;
            DisarmWatchdog();
            JobDisappeared?.Invoke(this, EventArgs.Empty);
        }

        private void OnLeaseLost()
        {
            JobHandle = Handle.Null;
            LeaseToken = string.Empty;
            DisarmWatchdog();
            LeaseLost?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        ///     Stops monitoring the job and fires up <see cref="E:Sitecore.Jobs.AsyncUI.JobMonitor.JobFinished" /> event.
        /// </summary>
        private void OnJobFinished(RunnerOutput runnerOutput)
        {
            JobHandle = Handle.Null;
            LeaseToken = string.Empty;
            DisarmWatchdog();
            var eventArgs = new SessionCompleteEventArgs { RunnerOutput = runnerOutput };
            JobFinished?.Invoke(this, eventArgs);
        }

        /// <summary>
        ///     Starts the job. A non-null <paramref name="recoveryOrigin" />
        ///     registers it with InteractiveJobRegistry so a page reloaded after
        ///     the browser discarded the tab can re-attach to it.
        /// </summary>
        public void Start(string name, string category, ThreadStart task, Language language = null, User user = null,
            IJobOptions options = null, InteractiveJobOrigin? recoveryOrigin = null,
            NameValueCollection launchContext = null)
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
            LeaseToken = string.Empty;

            if (recoveryOrigin.HasValue)
            {
                // Registered under the interactive user driving this page, not
                // the (possibly delegated) user the job runs as.
                var entry = InteractiveJobRegistry.Register(jobManager.GetJob(JobHandle), JobHandle,
                    Sitecore.Context.User.Name, InteractiveJobRegistry.CurrentTabId, recoveryOrigin.Value,
                    launchContext);
                LeaseToken = entry.LeaseToken;
            }

            ExpectedSeq++;
            ScheduleCallback();
        }

        /// <summary>
        ///     Binds this monitor to a job another page started, using a lease
        ///     already claimed from InteractiveJobRegistry. Re-shows the dialog
        ///     the job is waiting on, if any; otherwise resumes polling, which
        ///     drains whatever the job queued meanwhile, including completion.
        /// </summary>
        public void Adopt(InteractiveJobEntry entry)
        {
            Assert.ArgumentNotNull(entry, "entry");
            JobHandle = Handle.Parse(entry.Handle);
            LeaseToken = entry.LeaseToken;
            SetViewStateString("taskMonitorSessionId", entry.SessionKey ?? string.Empty);
            ExpectedSeq++;

            var pending = entry.PendingMessage;
            if (pending != null)
            {
                pending.Execute();
            }
            else
            {
                Active = true;
            }
        }

        /// <summary>
        ///     Polls once after <paramref name="delay" /> ms even while the
        ///     monitor is inactive (e.g. to drain output during a breakpoint).
        /// </summary>
        public void PollOnce(int delay)
        {
            SheerResponse.Timer($"{CheckMessage}(id={ID},seq={ExpectedSeq})", delay);
        }

        /// <summary>
        ///     Schedules the callback.
        /// </summary>
        private void ScheduleCallback()
        {
            if (Active)
            {
                SheerResponse.Timer($"{CheckMessage}(id={ID},seq={ExpectedSeq})", PollDelay);
                ArmWatchdog();
            }
        }

        private void ArmWatchdog()
        {
            var handle = HttpUtility.JavaScriptStringEncode(JobHandle.ToString(), true);
            SheerResponse.Eval(
                $"if(window.speJobWatchdog){{speJobWatchdog.arm('{ID}',{ExpectedSeq},{PollDelay},{handle});}}");
        }

        private void HoldWatchdog()
        {
            var heartbeat = InteractiveJobRegistry.HeartbeatSeconds * 1000;
            SheerResponse.Eval($"if(window.speJobWatchdog){{speJobWatchdog.hold('{ID}',{heartbeat});}}");
        }

        private void DisarmWatchdog()
        {
            SheerResponse.Eval($"if(window.speJobWatchdog){{speJobWatchdog.disarm('{ID}');}}");
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
