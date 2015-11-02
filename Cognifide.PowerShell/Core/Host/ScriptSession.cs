using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Web;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Provider;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;
using Sitecore.Web.UI.Sheer;
using Version = System.Version;

namespace Cognifide.PowerShell.Core.Host
{
    public class ScriptSession : IDisposable
    {
        public enum ExceptionStringFormat
        {
            Default,
            Html,
            Console
        }

        private const string HtmlExceptionFormatString =
            "<div class='white-space:normal; width:70%;'>{1}</div>{0}<strong>Of type</strong>: {3}{0}<strong>Stack trace</strong>:{0}{2}{0}";

        private const string HtmlInnerExceptionPrefix = "{0}<strong>Inner Exception:</strong>{1}";
        private const string HtmlLineEndFormat = "<br/>";

        private const string ConsoleExceptionFormatString =
            "[[;#f00;#000]Exception: {0}]\r\n" + "[[;#f00;#000]Type: {1}]\r\n" + "[[;#f00;#000]Stack trace:\r\n{2}]";

        private const string ConsoleInnerExceptionPrefix = "{0}[[b;#fff;#F00]Inner ] {1}";
        private const string ConsoleLineEndFormat = "\r\n";

        private const string ExceptionFormatString = "{1}{0}Of type: {3}{0}Stack trace:{0}{2}{0}";
        private const string ExceptionLineEndFormat = "\r\n";

        private static readonly SessionStateFormatEntry formats = new SessionStateFormatEntry(HttpRuntime.AppDomainAppPath +
                                                   @"sitecore modules\PowerShell\Assets\Sitecore.Views.ps1xml");

        private static readonly SessionStateTypeEntry types = new SessionStateTypeEntry(HttpRuntime.AppDomainAppPath +
                                               @"sitecore modules\PowerShell\Assets\Sitecore.Types.ps1xml");

        private bool disposed;
        private bool initialized;
        //private Pipeline pipeline;
        private readonly ScriptingHost host;
        private static InitialSessionState state;
        private bool abortRequested;
        private Exception errorFatal;
        private bool isRunspaceOpenedOrBroken;
        private System.Management.Automation.PowerShell powerShell;

        internal string JobScript { get; set; }
        internal JobOptions JobOptions { get; set; }
        public List<object> JobResultsStore { get; set; }

        static ScriptSession()
        {
            var typeAccelerators = typeof(PSObject).Assembly.GetType("System.Management.Automation.TypeAccelerators");
            MethodInfo mi = typeAccelerators.GetMethod("Add", BindingFlags.Public | BindingFlags.Static);

            foreach (var accelerator in TypeAccelerators.Accelerators)
            {
                mi.Invoke(null, new object[] { accelerator.Key, accelerator.Value });
            }
        }
        internal ScriptSession(string applianceType, bool personalizedSettings)
        {
            // Create and open a PowerShell runspace.  A runspace is a container 
            // that holds PowerShell pipelines, and provides access to the state
            // of the Runspace session (aka SessionState.)
            ApplianceType = applianceType;
            Settings = ApplicationSettings.GetInstance(ApplianceType, personalizedSettings);
            host = new ScriptingHost(Settings, SpeInitialSessionState);
            host.Runspace.StateChanged += OnRunspaceStateEvent;
            NewPowerShell();

            host.Runspace.Open();
            if (!initialized)
            {
                Initialize();
            }

            Runspace.DefaultRunspace = host.Runspace;

            // complete opening
            if (Runspace.DefaultRunspace == null)
            {
                //! wait while loading
                while (!isRunspaceOpenedOrBroken)
                    Thread.Sleep(100);

                //! set default runspace for handlers
                //! it has to be done in main thread
                Runspace.DefaultRunspace = host.Runspace;
            }

            if (Settings.UseTypeInfo)
            {
                Output.Clear();
            }
        }

        private void OnRunspaceStateEvent(object sender, RunspaceStateEventArgs e)
        {
            //! Carefully process events other than 'Opened'.
            if (e.RunspaceStateInfo.State != RunspaceState.Opened)
            {
                // alive? do nothing, wait for other events
                if (e.RunspaceStateInfo.State != RunspaceState.Broken)
                    return;

                // broken; keep an error silently
                errorFatal = e.RunspaceStateInfo.Reason;

                //! Set the broken flag, waiting threads may continue.
                //! The last code, Invoking() may be waiting for this.
                isRunspaceOpenedOrBroken = true;
                return;
            }
            Engine = host.Runspace.SessionStateProxy.PSVariable.GetValue("ExecutionContext") as EngineIntrinsics;

        }

        internal bool IsRunning
        {
            get { return powerShell != null && powerShell.InvocationStateInfo.State == PSInvocationState.Running; }
        }

        internal System.Management.Automation.PowerShell NewPowerShell()
        {
            if (IsRunning)
                return powerShell.CreateNestedPowerShell();

            powerShell = System.Management.Automation.PowerShell.Create();
            powerShell.Runspace = host.Runspace;
            return powerShell;
        }

        public InitialSessionState SpeInitialSessionState
        {
            get
            {
                if (state == null)
                {
                    state = InitialSessionState.CreateDefault();
                    state.AuthorizationManager = new AuthorizationManager("Sitecore.PowerShell");

                    state.Commands.Add(CognifideSitecorePowerShellSnapIn.SessionStateCommandlets);
                    if (Settings.UseTypeInfo)
                    {
                        state.Types.Add(types);
                        state.Formats.Add(formats);
                    }
                    state.ThreadOptions = PSThreadOptions.UseCurrentThread;
                    state.ApartmentState = Thread.CurrentThread.GetApartmentState();
                    foreach (var key in PredefinedVariables.Variables.Keys)
                    {
                        state.Variables.Add(new SessionStateVariableEntry(key, PredefinedVariables.Variables[key], "Sitecore PowerShell Extensions Predefined Variable"));
                    }
                    // PS 5 only?
                    //state.ExecutionPolicy = ExecutionPolicy.Bypass;
                    state.UseFullLanguageModeInDebugger = false;
                    PsSitecoreItemProvider.AppendToSessionState(state);                    
                }
                return state;
            }
        }

        public static Version PsVersion { get; private set; }

        public bool AutoDispose
        {
            get { return host.AutoDispose; }
            internal set { host.AutoDispose = value; }
        }

        public bool CloseRunner
        {
            get { return host.CloseRunner; }
            internal set { host.CloseRunner = value; }
        }

        public string ID
        {
            get { return host.SessionId; }
            internal set { host.SessionId = value; }
        }

        public bool Interactive
        {
            get { return host.Interactive; }
            internal set { host.Interactive = value; }
        }


        public string UserName
        {
            get { return host.User; }
            internal set { host.User = value; }
        }

        public string JobName
        {
            get { return host.JobName; }
            internal set { host.JobName = value; }
        }

        public string Key { get; internal set; }
        public ApplicationSettings Settings { get; }
        public OutputBuffer Output => host.Output;
        public RunspaceAvailability State => host.Runspace.RunspaceAvailability;
        public string ApplianceType { get; set; }
        public bool Debugging { get; set; }
        public string DebugFile { get; set; }
        internal EngineIntrinsics Engine { get; set; }

        public string CurrentLocation
        {
            get
            {
                try
                { 
                    return Engine.SessionState.Path.CurrentLocation.Path;
                }
                catch // above can cause problems that we don't really care about.
                {
                    return string.Empty;
                }
            }
        }

        public void SetVariable(string varName, object varValue)
        {
            lock (this)
            {
                Engine.SessionState.PSVariable.Set(varName, varValue);
            }
        }

        public object GetVariable(string varName)
        {
            lock (this)
            {
                return Engine.SessionState.PSVariable.GetValue(varName);
            }
        }

        public object GetDebugVariable(string varName)
        {
            lock (this)
            {
                try
                {
                    var value = Engine.SessionState.PSVariable.GetValue(varName);
                    return value ?? "undefined";
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        public void SetBreakpoints(string scriptPath, IEnumerable<int> breakpoints)
        {
            lock (this)
            {
                var bPointScript = string.Empty;
                foreach (var breakpoint in breakpoints)
                {
                    bPointScript +=
                        $"Set-PSBreakpoint -Script {scriptPath} -Line {breakpoint+1}\n";
                }
                bPointScript += "Set-PSDebug -Trace 2 -Step";
                ExecuteScriptPart(bPointScript, false, true, false);
            }
        }

        private void DebuggerOnBreakpointUpdated(object sender, BreakpointUpdatedEventArgs args)
        {
            if (Interactive)
            {
                if (args.Breakpoint is LineBreakpoint)
                {
                    var breakpoint = args.Breakpoint as LineBreakpoint;
                    if (string.Equals(breakpoint.Script, DebugFile, StringComparison.OrdinalIgnoreCase))
                    {
                        var message = Message.Parse(this, "ise:setbreakpoint");
                        message.Arguments.Add("Line", (breakpoint.Line - 1).ToString());
                        message.Arguments.Add("Action", args.UpdateType.ToString());
                        SendUiMessage(message);
                    }
                }
            }
        }

        private void SendUiMessage(Message message)
        {
            var sheerMessage = new SendMessageMessage(message, false);
            if (JobContext.IsJob)
            {
                message.Arguments.Add("JobId", Key);
                JobContext.MessageQueue.PutMessage(sheerMessage);
            }
            else
            {
                sheerMessage.Execute();
            }
        }

        private void DebuggerOnDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            if (((ScriptingHostUserInterface) host.UI).CheckSessionCanDoInteractiveAction(nameof(DebuggerOnDebuggerStop)))
            {
                InvokeInNewPowerShell("Get-PSBreakpoint -Variable SpeDebug | Remove-PSBreakpoint", OutTarget.OutNull);
                foreach (var breakpoint in args.Breakpoints)
                {
                    var message = Message.Parse(this, "ise:breakpointhit");
                    message.Arguments.Add("Line", (args.InvocationInfo.ScriptLineNumber-1).ToString());
                    message.Arguments.Add("HitCount", breakpoint.HitCount.ToString());
                    SendUiMessage(message);
                    NextDebugResumeAction = string.Empty;
                    while (string.IsNullOrEmpty(NextDebugResumeAction) && !abortRequested)
                    {
                        if (ImmediateCommand != null)
                        {
                            InvokeInRunningSession();
                        }
                        else
                        {
                            Thread.Sleep(100);
                        }
                    }
                    DebuggerResumeAction action;
                    Enum.TryParse(NextDebugResumeAction, true, out action);
                    args.ResumeAction = action;
                    if (action != DebuggerResumeAction.Continue)
                    {
                        InvokeInNewPowerShell("Set-PSBreakpoint -Variable SpeDebug -Mode Read", OutTarget.OutNull);
                        SetVariable("SpeDebug", true);
                        GetVariable("SpeDebug");
                    }
                }
            }
        }

        private void InvokeInRunningSession()
        {
            try
            {
                if (ImmediateCommand != null)
                {
                    if (ImmediateCommand is Command)
                    {
                        InvokeInNewPowerShell(ImmediateCommand as Command, OutTarget.OutDefault);
                    }
                    else
                    {
                        InvokeInNewPowerShell(ImmediateCommand as string, OutTarget.OutDefault);
                    }
                }
            }
            finally
            {
                ImmediateCommand = null;
            }

        }

        public bool TryInvokeInRunningSession(string script, bool stringOutput = false)
        {
            List<object> results;
            return TryInvokeInRunningSessionInternal(script, out results, stringOutput);
        }

        public bool TryInvokeInRunningSession(string script, out List<object> results, bool stringOutput = false)
        {
            return TryInvokeInRunningSessionInternal(script, out results, stringOutput);
        }

        public bool TryInvokeInRunningSession(Command command, bool stringOutput = false)
        {
            List<object> results;
            return TryInvokeInRunningSessionInternal(command, out results, stringOutput);
        }

        public bool TryInvokeInRunningSession(Command command, out List<object> results, bool stringOutput = false)
        {
            return TryInvokeInRunningSessionInternal(command, out results, stringOutput);
        }

        private bool TryInvokeInRunningSessionInternal(object executable, out List<object> results, bool stringOutput)
        {
            if (Debugging)
            {
                ImmediateCommand = executable;
                var tries = 20;
                while (ImmediateCommand != null && tries > 0)
                {
                    Thread.Sleep(100);
                    tries--;
                }
                results = ImmediateResults.BaseList<object>();
                return tries > 0;
            }
            else
            {
                var command = executable as Command;
                results = command != null
                    ? InvokeInNewPowerShell(command, OutTarget.OutNone).BaseList<object>()
                    : ExecuteScriptPart(executable as string, stringOutput, true, true);
                return true;
            }
        }

        public enum OutTarget
        {
            OutDefault,
            OutHost,
            OutNull,
            OutNone
        }

        public Collection<PSObject> InvokeInNewPowerShell(Command command, OutTarget target)
        {
            return InvokeInNewPowerShell(ps => ps.Commands.AddCommand(command), target);
        }

        public Collection<PSObject> InvokeInNewPowerShell(string script, OutTarget target)
        {
            return InvokeInNewPowerShell(ps => ps.Commands.AddScript(script), target);
        }

        private Collection<PSObject> InvokeInNewPowerShell(Func<System.Management.Automation.PowerShell, PSCommand> action, OutTarget target)
        {
            using (var ps = NewPowerShell())
            {
                var psc = action(ps);
                switch (target)
                {
                    case (OutTarget.OutNull):
                        psc.AddCommand(OutNullCommand);
                        break;
                    case (OutTarget.OutHost):
                        psc.AddCommand(OutHostCommand);
                        break;
                    case (OutTarget.OutDefault):
                        psc.AddCommand(OutDefaultCommand);
                        break;
                }
                var results = ps.Invoke();
                return results;
            }
        }

        private object ImmediateCommand { get; set; }
        private object ImmediateResults { get; set; }

        public string NextDebugResumeAction { get; set; } = string.Empty;


        public List<PSVariable> Variables
        {
            get
            {
                lock (this)
                {                    
                    return ExecuteScriptPart("Get-Variable", false, true).Cast<PSVariable>().ToList();
                }
            }
        }

        public void Initialize()
        {
            Initialize(false);
        }

        public void Initialize(bool reinitialize)
        {
            lock (this)
            {
                if (initialized && !reinitialize) return;

                initialized = true;
                UserName = User.Current.Name;
                var proxy = host.Runspace.SessionStateProxy;
                proxy.SetVariable("me", UserName);
                proxy.SetVariable("HttpContext", HttpContext.Current);
                if (HttpContext.Current != null)
                {
                    proxy.SetVariable("request", HttpContext.Current.Request);
                    proxy.SetVariable("response", HttpContext.Current.Response);
                }

                proxy.SetVariable("ClientData", Context.ClientData);
                try
                {
                    proxy.SetVariable("ClientPage", Context.ClientPage);
                }
                catch
                {
                    Log.Warn("Unable to set the ClientPage variable.", this);
                }
                proxy.SetVariable("HostSettings", Settings);
                proxy.SetVariable("ScriptSession", this);

                if (PsVersion == null)
                {
                    PsVersion = (Version)ExecuteScriptPart("$PSVersionTable.PSVersion", false, true)[0];
                }

                ExecuteScriptPart(RenamedCommands.AliasSetupScript, false, true, false);
            }
        }

        public ScriptBlock GetScriptBlock(string scriptBlock)
        {
            return host.Runspace.SessionStateProxy.InvokeCommand.NewScriptBlock(scriptBlock);
        }

        public static string GetDataContextSwitch(Item item)
        {
            return item != null
                ? string.Format(
                    "cd \"{0}:{1}\"\n", item.Database.Name,
                    item.Paths.Path.Replace("/", "\\").Substring(9))
                : string.Empty;
        }

        public List<object> ExecuteScriptPart(string script)
        {
            return ExecuteScriptPart(script, true, false);
        }

        public List<object> ExecuteScriptPart(Item scriptItem, bool stringOutput)
        {
            SetExecutedScript(scriptItem);
            var script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                : string.Empty;
            return ExecuteScriptPart(script, stringOutput, false);
        }

        public List<object> ExecuteScriptPart(string script, bool stringOutput)
        {
            return ExecuteScriptPart(script, stringOutput, false);
        }

        internal List<object> ExecuteScriptPart(string script, bool stringOutput, bool internalScript)
        {
            return ExecuteScriptPart(script, stringOutput, internalScript, true);
        }

        internal List<object> ExecuteScriptPart(string script, bool stringOutput, bool internalScript,
            bool marshalResults)
        {
            if (string.IsNullOrWhiteSpace(script) || State == RunspaceAvailability.Busy)
            {
                return null;
            }

            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = host.Runspace;
            }

            Log.Info("Executing a Sitecore PowerShell Extensions script.", this);
            Log.Debug(script, this);

            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            return SpeTimer.Measure("script execution", () =>
            {
                try
                {
                    using (powerShell = NewPowerShell())
                    {
                        powerShell.Commands.AddScript(script);
                        return ExecuteCommand(stringOutput, marshalResults);
                    }
                }
                finally
                {
                    powerShell = null;
                }
            });
        }

        internal List<object> ExecuteCommand(Command command, bool stringOutput, bool internalScript)
        {
            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            try
            {
                using (powerShell = NewPowerShell())
                {
                    powerShell.Commands.AddCommand(command);
                    return ExecuteCommand(stringOutput);
                }
            }
            finally
            {
                powerShell = null;
            }
        }

        public void Abort()
        {
            powerShell?.Stop();
            abortRequested = true;
        }

        public static Command OutDefaultCommand
        {
            get
            {
                var command = new Command("Out-Default");
                command.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error;
                return command;
            }
        }
        /// <summary>
        /// command for formatted output of everything.
        /// </summary>
        /// <remarks>
        /// "Out-Default" is not suitable for external apps, output goes to console.
        /// </remarks>
        public static Command OutHostCommand
        {
            get
            {
                var command = new Command("Out-Host");
                command.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error;
                return command;
            }
        }

        /// <summary>
        /// command for formatted output of everything.
        /// </summary>
        /// <remarks>
        /// "Out-Default" is not suitable for external apps, output goes to console.
        /// </remarks>
        public static Command OutNullCommand
        {
            get
            {
                var command = new Command("Out-Null");
                command.MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error;
                return command;
            }
        }

        private List<object> ExecuteCommand(bool stringOutput, bool marshallResults = true)
        {
            JobName = Context.Job?.Name;

            if (stringOutput)
            {
                powerShell.Commands.AddCommand(OutDefaultCommand);
            }

            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = host.Runspace;
            }

            if (Interactive && Debugging)
            {
                host.Runspace.Debugger.DebuggerStop += DebuggerOnDebuggerStop;
                host.Runspace.Debugger.BreakpointUpdated += DebuggerOnBreakpointUpdated;
                host.Runspace.Debugger.SetDebuggerStepMode(true);

                var message = Message.Parse(this, "ise:debugstart");
                SendUiMessage(message);
            }
            powerShell.Runspace.StateChanged += PipelineStateChanged;
            abortRequested = false;
            // execute the commands in the pipeline now
            var execResults = powerShell.Invoke();

            if (execResults != null && execResults.Any())
            {
                foreach (
                    var record in
                        execResults.Select(p => p.BaseObject).OfType<ErrorRecord>().Select(result => result))
                {
                    Log.Error(record + record.InvocationInfo.PositionMessage, this);
                }
            }

            if (Interactive && Debugging)
            {
                host.Runspace.Debugger.DebuggerStop -= DebuggerOnDebuggerStop;
                host.Runspace.Debugger.BreakpointUpdated -= DebuggerOnBreakpointUpdated;
                host.Runspace.Debugger.SetDebuggerStepMode(true);

                var message = Message.Parse(this, "ise:debugend");
                SendUiMessage(message);
                Debugging = false;
            }

            JobName = string.Empty;
            return marshallResults
                ? execResults?.Select(p => p.BaseObject).ToList()
                : execResults?.Cast<object>().ToList();
        }

        private void PipelineStateChanged(object sender, RunspaceStateEventArgs e)
        {
        }

        public string GetExceptionString(Exception ex, ExceptionStringFormat format = ExceptionStringFormat.Default)
        {
            var stacktrace = ex.StackTrace;
            var exceptionPrefix = string.Empty;
            var exceptionFormat = ExceptionFormatString;
            var lineEndFormat = ExceptionLineEndFormat;
            switch (format)
            {
                case ExceptionStringFormat.Html:
                    lineEndFormat = HtmlLineEndFormat;
                    stacktrace = stacktrace.Replace("\n", lineEndFormat);
                    exceptionPrefix = HtmlInnerExceptionPrefix;
                    exceptionFormat = HtmlExceptionFormatString;
                    break;
                case ExceptionStringFormat.Console:
                    lineEndFormat = ConsoleLineEndFormat;
                    stacktrace = stacktrace.Replace("[", "%((%").Replace("]", "%))%");
                    exceptionPrefix = ConsoleInnerExceptionPrefix;
                    exceptionFormat = ConsoleExceptionFormatString;
                    break;
            }
            var exception = string.Empty;
            exception += string.Format(exceptionFormat, lineEndFormat, ex.Message, stacktrace, ex.GetType());
            if (ex.InnerException != null)
            {
                exception += string.Format(exceptionPrefix, lineEndFormat, GetExceptionString(ex.InnerException));
            }
            return exception;
        }

        public void SetItemLocationContext(Item item)
        {
            if (item != null)
            {
                SetVariable("SitecoreContextItem", item);
                var contextScript = GetDataContextSwitch(item);
                ExecuteScriptPart(contextScript, false, true, false);
            }
        }

        public void SetExecutedScript(string database, string path)
        {
            if (!string.IsNullOrEmpty(database) && !string.IsNullOrEmpty(path))
            {
                var scriptItem = Factory.GetDatabase(database).GetItem(new ID(path));
                SetExecutedScript(scriptItem);
            }
        }

        public void SetExecutedScript(Item scriptItem)
        {
            if (scriptItem != null)
            {
                SetVariable("PSScriptRoot", scriptItem.Parent.GetProviderPath());
                SetVariable("PSCommandPath", scriptItem.GetProviderPath());
                SetVariable("PSScript", scriptItem);
            }
        }

        #region IDisposable logic

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ScriptSession()
        {
            Dispose(false);
        }

        public void Close()
        {
            host.Runspace.Dispose();
            disposed = true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    Close();
                    if (!string.IsNullOrEmpty(Key))
                    {
                        ScriptSessionManager.RemoveSession(Key);
                    }
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        #endregion
    }
}