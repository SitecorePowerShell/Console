using System;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents an SPE Remoting Client item from the content tree.
    /// A client binds a credential (shared secret in v1; OAuth client id in a
    /// later phase) to a remoting policy, optional user impersonation, and
    /// request throttling.
    /// </summary>
    public class RemotingClient
    {
        // Sitecore item GUID of the SSC item. Stable across renames and free of
        // delimiter characters, so this is the safe value to emit as the
        // remotingClient field in audit lines (see Spe.Core.Diagnostics.LogSanitizer).
        public Guid Id { get; }
        public string Name { get; }
        public string AccessKeyId { get; }
        public string SharedSecret { get; }
        public bool Enabled { get; }
        public string Policy { get; }
        public string ImpersonateUser { get; }
        public int RequestLimit { get; }
        public int ThrottleWindowSeconds { get; }
        public string ThrottleAction { get; }
        public DateTime? Expires { get; }

        public RemotingClient(
            Guid id,
            string name,
            string accessKeyId,
            string sharedSecret,
            bool enabled,
            string policy,
            string impersonateUser,
            int requestLimit,
            int throttleWindowSeconds,
            string throttleAction,
            DateTime? expires)
        {
            Id = id;
            Name = name;
            AccessKeyId = accessKeyId;
            SharedSecret = sharedSecret;
            Enabled = enabled;
            Policy = policy;
            ImpersonateUser = impersonateUser;
            RequestLimit = requestLimit;
            ThrottleWindowSeconds = throttleWindowSeconds;
            ThrottleAction = string.IsNullOrEmpty(throttleAction) ? "Block" : throttleAction;
            Expires = expires;
        }

        public bool HasThrottle => RequestLimit > 0 && ThrottleWindowSeconds > 0;
        public bool HasImpersonation => !string.IsNullOrEmpty(ImpersonateUser);
        public bool HasPolicy => !string.IsNullOrEmpty(Policy);
        public bool HasAccessKeyId => !string.IsNullOrEmpty(AccessKeyId);
        public bool IsExpired => Expires.HasValue && DateTime.UtcNow > Expires.Value;
    }
}
