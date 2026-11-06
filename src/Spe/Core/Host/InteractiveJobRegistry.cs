using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Host
{
    public enum InteractiveJobOrigin
    {
        Runner,
        Ise,
        ListView
    }

    public enum LeaseState
    {
        NotRegistered,
        Valid,
        Lost
    }

    /// <summary>
    ///     One interactive job a browser page is (or was) rendering. Holds the
    ///     IJob reference so its MessageQueue - and the CompleteMessage or
    ///     Show-ListView messages queued while the tab slept - outlives the
    ///     10 second AfterLife after which JobManager forgets the job.
    /// </summary>
    public class InteractiveJobEntry
    {
        internal IJob JobReference;

        public string Handle { get; internal set; }
        public string UserName { get; internal set; }
        public string SessionKey { get; internal set; }
        public InteractiveJobOrigin Origin { get; internal set; }
        public NameValueCollection LaunchContext { get; internal set; }
        public IMessage PendingMessage { get; internal set; }
        public string PendingMessageId { get; internal set; }
        public string OwnerTabId { get; internal set; }
        public string OwnerPageId { get; internal set; }
        public string LeaseToken { get; internal set; }
        public DateTime LastSeenUtc { get; internal set; }

        public IJob Job
        {
            get
            {
                if (JobReference == null)
                {
                    JobReference = TypeResolver.ResolveFromCache<IJobManager>()
                        .GetJob(Sitecore.Handle.Parse(Handle));
                }

                return JobReference;
            }
        }

        public bool IsFinished => Job?.IsDone ?? true;
    }

    /// <summary>
    ///     Per-user registry of interactive jobs started through SpeJobMonitor.
    ///     Lets a page that comes back from a frozen or discarded browser tab
    ///     find and re-attach to its job, and makes sure only one page (the
    ///     current lease holder) renders a job at a time.
    /// </summary>
    public static class InteractiveJobRegistry
    {
        public const string TabIdField = "__SPETABID";
        public const string PageIdField = "__SPEPAGEID";

        private static readonly object Sync = new object();
        private static readonly Dictionary<string, InteractiveJobEntry> Entries =
            new Dictionary<string, InteractiveJobEntry>(StringComparer.OrdinalIgnoreCase);

        public static int UnattendedRetentionMinutes =>
            Sitecore.Configuration.Settings.GetIntSetting("Spe.InteractiveJobs.UnattendedRetentionMinutes", 30);

        public static int ClaimAbandonedSeconds =>
            Sitecore.Configuration.Settings.GetIntSetting("Spe.InteractiveJobs.ClaimAbandonedSeconds", 120);

        public static int OwnTabReclaimSeconds =>
            Sitecore.Configuration.Settings.GetIntSetting("Spe.InteractiveJobs.OwnTabReclaimSeconds", 5);

        public static int HeartbeatSeconds =>
            Sitecore.Configuration.Settings.GetIntSetting("Spe.InteractiveJobs.HeartbeatSeconds", 15);

        /// <summary>
        ///     Browser tab identity posted by SpeJobRecovery.js with every Sheer
        ///     request. Empty when the page does not carry the script.
        /// </summary>
        public static string CurrentTabId =>
            HttpContext.Current?.Request.Form[TabIdField] ?? string.Empty;

        /// <summary>
        ///     Identity of the top-level page load the request came from (new on
        ///     every reload). Discovery never claims a job the requesting page
        ///     itself holds - it is not orphaned, just busy.
        /// </summary>
        public static string CurrentPageId =>
            HttpContext.Current?.Request.Form[PageIdField] ?? string.Empty;

        public static InteractiveJobEntry Register(IJob job, Handle handle, string userName, string tabId,
            InteractiveJobOrigin origin, NameValueCollection launchContext)
        {
            var entry = new InteractiveJobEntry
            {
                JobReference = job,
                Handle = handle.ToString(),
                UserName = userName,
                Origin = origin,
                LaunchContext = launchContext ?? new NameValueCollection(),
                OwnerTabId = tabId ?? string.Empty,
                OwnerPageId = CurrentPageId,
                LeaseToken = Guid.NewGuid().ToString("N"),
                LastSeenUtc = DateTime.UtcNow
            };

            lock (Sync)
            {
                ExpireStale();
                Entries[entry.Handle] = entry;
            }

            return entry;
        }

        public static InteractiveJobEntry Get(string handle)
        {
            if (string.IsNullOrEmpty(handle)) return null;
            lock (Sync)
            {
                return Entries.TryGetValue(handle, out var entry) ? entry : null;
            }
        }

        public static void SetSessionKey(string handle, string sessionKey)
        {
            var entry = Get(handle);
            if (entry != null && !string.IsNullOrEmpty(sessionKey))
            {
                entry.SessionKey = sessionKey;
            }
        }

        public static void Remove(string handle)
        {
            if (string.IsNullOrEmpty(handle)) return;
            lock (Sync)
            {
                Entries.Remove(handle);
            }
        }

        /// <summary>
        ///     Validates that the calling page still holds the job's lease and
        ///     records that it is alive.
        /// </summary>
        public static LeaseState Touch(string handle, string leaseToken)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(handle) || !Entries.TryGetValue(handle, out var entry))
                {
                    return LeaseState.NotRegistered;
                }

                if (!string.Equals(entry.LeaseToken, leaseToken, StringComparison.Ordinal))
                {
                    return LeaseState.Lost;
                }

                entry.LastSeenUtc = DateTime.UtcNow;
                return LeaseState.Valid;
            }
        }

        public static void SetPending(string handle, IMessage message, string messageId)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(handle) || !Entries.TryGetValue(handle, out var entry)) return;
                entry.PendingMessage = message;
                entry.PendingMessageId = messageId;
            }
        }

        /// <summary>
        ///     Claims the right to post the answer to a dialog. True for jobs the
        ///     registry does not track, and for the first answer to the dialog
        ///     currently pending; false for a second answer (the same dialog
        ///     shown again in another window). MessageQueue.PutResult blocks
        ///     while a result is held, so a duplicate answer would hang the
        ///     request thread and feed the wrong answer to the next dialog.
        /// </summary>
        public static bool TryResolvePending(string handle, string messageId)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(handle) || !Entries.TryGetValue(handle, out var entry)) return true;
                if (string.IsNullOrEmpty(messageId) ||
                    !string.Equals(entry.PendingMessageId, messageId, StringComparison.Ordinal))
                {
                    return false;
                }

                entry.PendingMessage = null;
                entry.PendingMessageId = null;
                return true;
            }
        }

        /// <summary>
        ///     Claims every job of <paramref name="userName" /> a freshly loaded
        ///     page in tab <paramref name="tabId" /> should take over: its own
        ///     tab's jobs once their previous page went quiet, and anyone's
        ///     abandoned jobs. <paramref name="ownTabStillFresh" /> is true when
        ///     this tab owns jobs that were skipped only because their lease was
        ///     touched too recently - the caller retries shortly.
        /// </summary>
        public static List<InteractiveJobEntry> ClaimOrphans(string userName, string tabId,
            Func<InteractiveJobOrigin, bool> originFilter, out bool ownTabStillFresh, int max = int.MaxValue)
        {
            var claimed = new List<InteractiveJobEntry>();
            ownTabStillFresh = false;
            lock (Sync)
            {
                ExpireStale();
                var pageId = CurrentPageId;
                foreach (var entry in Entries.Values.Where(e => IsSameUser(e, userName) && originFilter(e.Origin)))
                {
                    if (!string.IsNullOrEmpty(pageId) &&
                        string.Equals(entry.OwnerPageId, pageId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (claimed.Count < max && CanClaim(entry, tabId))
                    {
                        Claim(entry, tabId);
                        claimed.Add(entry);
                    }
                    else if (IsOwnTab(entry, tabId))
                    {
                        ownTabStillFresh = true;
                    }
                }
            }

            return claimed.OrderBy(e => e.LastSeenUtc).ToList();
        }

        /// <summary>
        ///     Claims one job by handle (used by a Runner that reloads itself into
        ///     reattach mode without a claim token). Null when the job is unknown,
        ///     belongs to someone else, or a live page still holds it.
        /// </summary>
        public static InteractiveJobEntry TryClaim(string handle, string userName, string tabId)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(handle) || !Entries.TryGetValue(handle, out var entry)) return null;
                if (!IsSameUser(entry, userName) || !CanClaim(entry, tabId)) return null;
                Claim(entry, tabId);
                return entry;
            }
        }

        /// <summary>
        ///     Lets the Runner opened by discovery take over the lease discovery
        ///     claimed for it. Null if the token was superseded meanwhile.
        /// </summary>
        public static InteractiveJobEntry TryAdopt(string handle, string userName, string leaseToken)
        {
            lock (Sync)
            {
                if (string.IsNullOrEmpty(handle) || !Entries.TryGetValue(handle, out var entry)) return null;
                if (!IsSameUser(entry, userName) ||
                    !string.Equals(entry.LeaseToken, leaseToken, StringComparison.Ordinal))
                {
                    return null;
                }

                entry.LastSeenUtc = DateTime.UtcNow;
                return entry;
            }
        }

        public static bool BelongsTo(string handle, string userName)
        {
            var entry = Get(handle);
            return entry != null && IsSameUser(entry, userName);
        }

        private static bool CanClaim(InteractiveJobEntry entry, string tabId)
        {
            var quietFor = DateTime.UtcNow - entry.LastSeenUtc;
            if (IsOwnTab(entry, tabId) && quietFor.TotalSeconds >= OwnTabReclaimSeconds) return true;
            return quietFor.TotalSeconds >= ClaimAbandonedSeconds;
        }

        private static bool IsOwnTab(InteractiveJobEntry entry, string tabId) =>
            !string.IsNullOrEmpty(tabId) && string.Equals(entry.OwnerTabId, tabId, StringComparison.Ordinal);

        private static bool IsSameUser(InteractiveJobEntry entry, string userName) =>
            string.Equals(entry.UserName, userName, StringComparison.OrdinalIgnoreCase);

        private static void Claim(InteractiveJobEntry entry, string tabId)
        {
            entry.LeaseToken = Guid.NewGuid().ToString("N");
            entry.OwnerTabId = tabId ?? string.Empty;
            entry.OwnerPageId = CurrentPageId;
            entry.LastSeenUtc = DateTime.UtcNow;
        }

        // ponytail: lazy sweep on registry access, no timer thread; entries are
        // few (one per running interactive script) so a full scan is cheap.
        private static void ExpireStale()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-UnattendedRetentionMinutes);
            var expired = Entries.Values
                .Where(e => e.LastSeenUtc < cutoff && e.IsFinished)
                .ToList();
            foreach (var entry in expired)
            {
                Entries.Remove(entry.Handle);
                PowerShellLog.Info(
                    $"[Runner] action=unclaimedResultExpired user={entry.UserName} job={entry.Handle} script={entry.LaunchContext?["scriptId"]}");
            }
        }
    }
}
