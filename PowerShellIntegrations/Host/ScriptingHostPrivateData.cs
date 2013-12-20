namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    /// <summary>
    ///     This is a sample implementation of the PSHost abstract class for
    ///     console applications. Not all members are implemented. Those that
    ///     are not implemented throw a NotImplementedException exception or
    ///     return nothing.
    /// </summary>

    public class ScriptingHostPrivateData
    {
        internal ScriptingHostPrivateData(ScriptingHost host)
        {
            ScriptingHost = host;
        }
        public ScriptingHost ScriptingHost { private set; get; }
        public string SessionId { get { return ScriptingHost.SessionId; } }
    }
}