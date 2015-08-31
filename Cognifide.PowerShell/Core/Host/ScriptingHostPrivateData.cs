namespace Cognifide.PowerShell.Core.Host
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

        public ScriptingHost ScriptingHost { get; }

        public string SessionId => ScriptingHost.SessionId;

        public bool AutoDispose => ScriptingHost.AutoDispose;

        public bool Interactive => ScriptingHost.Interactive;

        public bool CloseRunner
        {
            get { return ScriptingHost.CloseRunner; }
            internal set { ScriptingHost.CloseRunner = value; }
        }
    }
}