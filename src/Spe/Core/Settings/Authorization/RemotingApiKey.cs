namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents an SPE Remoting API Key item from the content tree.
    /// Each key binds a shared secret to a restriction profile,
    /// optional user impersonation, and request throttling.
    /// </summary>
    public class RemotingApiKey
    {
        public string Name { get; }
        public string SharedSecret { get; }
        public bool Enabled { get; }
        public string Profile { get; }
        public string ImpersonateUser { get; }
        public int RequestLimit { get; }
        public int ThrottleWindowSeconds { get; }

        public RemotingApiKey(
            string name,
            string sharedSecret,
            bool enabled,
            string profile,
            string impersonateUser,
            int requestLimit,
            int throttleWindowSeconds)
        {
            Name = name;
            SharedSecret = sharedSecret;
            Enabled = enabled;
            Profile = profile;
            ImpersonateUser = impersonateUser;
            RequestLimit = requestLimit;
            ThrottleWindowSeconds = throttleWindowSeconds;
        }

        public bool HasThrottle => RequestLimit > 0 && ThrottleWindowSeconds > 0;
        public bool HasImpersonation => !string.IsNullOrEmpty(ImpersonateUser);
        public bool HasProfile => !string.IsNullOrEmpty(Profile);
    }
}
