using System;

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

        public ConsoleColor BackgroundColor
        {
            get
            {
                return ScriptingHost.UI.RawUI.BackgroundColor;
            }
            set
            {
                ScriptingHost.UI.RawUI.BackgroundColor = value;
            }
        }

        public ConsoleColor ForegroundColor
        {
            get
            {
                return ScriptingHost.UI.RawUI.ForegroundColor;
            }
            set
            {
                ScriptingHost.UI.RawUI.ForegroundColor = value;
            }
        }
        public ConsoleColor DebugBackgroundColor { get; set; } = ConsoleColor.DarkBlue;
        public ConsoleColor DebugForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor ErrorBackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor ErrorForegroundColor { get; set; } = ConsoleColor.Red;
        public ConsoleColor ProgressBackgroundColor { get; set; } = ConsoleColor.DarkCyan;
        public ConsoleColor ProgressForegroundColor { get; set; } = ConsoleColor.White;
        public ConsoleColor VerboseBackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor VerboseForegroundColor { get; set; } = ConsoleColor.Yellow;
        public ConsoleColor WarningBackgroundColor { get; set; } = ConsoleColor.Black;
        public ConsoleColor WarningForegroundColor { get; set; } = ConsoleColor.Yellow;
    }
}