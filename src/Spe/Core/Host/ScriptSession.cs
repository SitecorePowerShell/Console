using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Language;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Web;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Exceptions;
using Sitecore.Install.Serialization;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;
using Sitecore.Web.UI.Sheer;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Debugging;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Settings.Authorization;
using Spe.Core.Provider;
using Spe.Core.Settings;
using Spe.Core.Utility;
using Spe.Core.VersionDecoupling;
using Version = System.Version;

namespace Spe.Core.Host
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
            "[[;#FF9494;]Exception: {0}]\r\n" + "[[;#FF9494;]Type: {1}]\r\n" + "[[;#FF9494;]Stack trace:\r\n{2}]";

        private const string ConsoleInnerExceptionPrefix = "{0}[[b;#f00;]Inner ] {1}";
        private const string ConsoleLineEndFormat = "\r\n";

        private const string ExceptionFormatString = "{1}{0}Of type: {3}{0}Stack trace:{0}{2}{0}";
        private const string ExceptionLineEndFormat = "\r\n";

        private static readonly SessionStateFormatEntry formats = new SessionStateFormatEntry(HttpRuntime.AppDomainAppPath +
                                                   @"sitecore modules\PowerShell\Assets\Sitecore.Views.ps1xml");

        private static readonly SessionStateTypeEntry types = new SessionStateTypeEntry(HttpRuntime.AppDomainAppPath +
                                               @"sitecore modules\PowerShell\Assets\Sitecore.Types.ps1xml");

        private bool disposed;
        private bool initialized;
        private readonly ScriptingHost host;
        private static InitialSessionState state;
        private bool abortRequested;
        private bool isRunspaceOpenedOrBroken;
        private System.Management.Automation.PowerShell powerShell;
        private ScriptingHostPrivateData privateData;

        internal string JobScript { get; set; }
        internal IJobOptions JobOptions { get; set; }
        public List<object> JobResultsStore { get; set; }
        internal ScriptingHost Host => host;

        internal Stack<IMessage> DialogStack { get; } = new Stack<IMessage>(); 
        internal Hashtable[] DialogResults { get; set; }

        public ScriptingHostPrivateData PrivateData
        {
            get { return privateData ?? (privateData = host.PrivateData.BaseObject as ScriptingHostPrivateData); }
        }

        static ScriptSession()
        {
            TypeAccelerators.AddSitecoreAccelerators();
            using (var ps = System.Management.Automation.PowerShell.Create())
            {
                var psVersionTable = ps.Runspace.SessionStateProxy.GetVariable("PSVersionTable") as Hashtable;
                PsVersion = (Version)psVersionTable["PSVersion"];
            }

            // Ensure IOUtils created by touching IOUtils.SerializationContext
            using (new DatabaseSwitcher(Factory.GetDatabase("core")))
            {
                var context = IOUtils.SerializationContext;
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
            powerShell = NewPowerShell();

            host.Runspace.Open();
            if (!initialized)
            {
                Initialize();
            }

            
            if (Runspace.DefaultRunspace == null)
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

            // Session init - no client connected yet, silent clear.
            Output.ClearSilent();
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
                //errorFatal = e.RunspaceStateInfo.Reason;

                //! Set the broken flag, waiting threads may continue.
                //! The last code, Invoking() may be waiting for this.
                isRunspaceOpenedOrBroken = true;
                return;
            }
            Engine = host.Runspace.SessionStateProxy.PSVariable.GetValue("ExecutionContext") as EngineIntrinsics;

        }

        internal bool IsRunning => powerShell != null && powerShell.InvocationStateInfo.State == PSInvocationState.Running;

        private System.Management.Automation.PowerShell NewPowerShell()
        {
            if (IsRunning)
                return powerShell.CreateNestedPowerShell();

            var newPowerShell = System.Management.Automation.PowerShell.Create();
            newPowerShell.Runspace = host.Runspace;
            return newPowerShell;
        }

        public InitialSessionState SpeInitialSessionState
        {
            get
            {
                if (state != null) return state;

                state = InitialSessionState.CreateDefault();
                state.AuthorizationManager = new AuthorizationManager("Sitecore.PowerShell");

                state.Commands.Add(SpeSitecorePowerShellSnapIn.SessionStateCommandlets);
                state.Types.Add(types);
                state.Formats.Add(formats);
                state.ThreadOptions = PSThreadOptions.UseCurrentThread;
                state.ApartmentState = Thread.CurrentThread.GetApartmentState();
                foreach (var key in PredefinedVariables.Variables.Keys)
                {
                    state.Variables.Add(new SessionStateVariableEntry(key, PredefinedVariables.Variables[key],
                        "Sitecore PowerShell Extensions Predefined Variable"));
                }
                state.UseFullLanguageModeInDebugger = true;
                PsSitecoreItemProviderFactory.AppendToSessionState(state);
                return state;
            }
        }

        public static Version PsVersion { get; }

        public bool AutoDispose
        {
            get => host.AutoDispose;
            internal set => host.AutoDispose = value;
        }

        public bool CloseRunner
        {
            get => host.CloseRunner;
            internal set => host.CloseRunner = value;
        }

        public List<string> DeferredMessages => host.DeferredMessages;

        public string ID
        {
            get => host.SessionId;
            internal set => host.SessionId = value;
        }

        public bool Interactive
        {
            get => host.Interactive;
            set => host.Interactive = value;
        }


        public string UserName
        {
            get => host.User;
            private set => host.User = value;
        }

        public string JobName
        {
            get => host.JobName;
            internal set => host.JobName = value;
        }

        public List<ErrorRecord> LastErrors { get; set; }

        public string Key
        {
            get => host.SessionKey;
            internal set => host.SessionKey = value;
        }

        public ApplicationSettings Settings { get; }
        public OutputBuffer Output => host.Output;
        public RunspaceAvailability State => host.Runspace.RunspaceAvailability;
        public string ApplianceType { get; set; }
        public bool Debugging { get; set; }
        public string DebugFile { get; set; }
        public RestrictionProfile ActiveRestrictionProfile { get; set; }
        internal EngineIntrinsics Engine { get; set; }
        public bool DebuggingInBreakpoint { get; private set; }
        public int[] Breakpoints { get; set; } = new int[0];

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

        public void SetLanguageMode(PSLanguageMode mode)
        {
            host.Runspace.SessionStateProxy.LanguageMode = mode;
        }

        public void RemoveVariable(string varName)
        {
            lock (this)
            {
                Engine.SessionState.PSVariable.Remove(varName);
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
                    return value;
                }
                catch (Exception ex)
                {
                    return ex.Message;
                }
            }
        }

        /// <summary>
        /// PowerShell automatic variables that should be hidden from the ISE
        /// Variables panel entirely because they're engine-managed, not user-visible.
        /// </summary>
        private static readonly HashSet<string> EngineAutomaticVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "_", "?", "^", "$", "args", "ConfirmPreference", "ConsoleFileName",
            "DebugPreference", "EnabledExperimentalFeatures", "ErrorActionPreference",
            "ErrorView", "Error", "Event", "EventArgs", "EventSubscriber", "ExecutionContext",
            "false", "FormatEnumerationLimit", "foreach", "HOME", "Host", "InformationPreference",
            "input", "IsCoreCLR", "IsLinux", "IsMacOS", "IsWindows", "LASTEXITCODE",
            "Matches", "MaximumAliasCount", "MaximumDriveCount", "MaximumErrorCount",
            "MaximumFunctionCount", "MaximumHistoryCount", "MaximumVariableCount",
            "MyInvocation", "NestedPromptLevel", "null", "OFS", "OutputEncoding", "PID",
            "profile", "ProgressPreference", "PSBoundParameters", "PSCmdlet", "PSCommandPath",
            "PSCulture", "PSDebugContext", "PSDefaultParameterValues", "PSEdition",
            "PSEmailServer", "PSHOME", "PSItem", "PSNativeCommandArgumentPassing",
            "PSNativeCommandUseErrorActionPreference", "PSScriptRoot", "PSSenderInfo",
            "PSSessionApplicationName", "PSSessionConfigurationName", "PSSessionOption",
            "PSStyle", "PSUICulture", "PSVersionTable", "PWD", "Sender", "ShellId",
            "StackTrace", "switch", "this", "true", "VerbosePreference", "WarningPreference",
            "WhatIfPreference", "SitecoreContextItem"
        };

        /// <summary>
        /// SPE/Sitecore "built-in" variables that are set up by the session's
        /// Initialize() method and PredefinedVariables. They're shown in the
        /// Variables panel but grouped separately from user-created variables.
        /// Keep in sync with Initialize() in this class and PredefinedVariables.cs.
        /// </summary>
        private static readonly HashSet<string> SpeBuiltInVariableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "me", "HostSettings", "ScriptSession", "SitecoreAuthority",
            "AppPath", "AppVPath", "tempPath", "tmpPath",
            "SitecoreDataFolder", "SitecoreDebugFolder", "SitecoreLayoutFolder",
            "SitecoreLogFolder", "SitecoreMediaFolder", "SitecorePackageFolder",
            "SitecoreSerializationFolder", "SitecoreTempFolder", "SitecoreVersion"
        };

        /// <summary>
        /// Lazily-created CSharpCodeProvider for generating C# shorthand type
        /// names (e.g. "string" instead of "System.String", "object[]" instead
        /// of "System.Object[]") via GetTypeOutput.
        /// </summary>
        private static readonly Lazy<Microsoft.CSharp.CSharpCodeProvider> CSharpTypeFormatter =
            new Lazy<Microsoft.CSharp.CSharpCodeProvider>(() => new Microsoft.CSharp.CSharpCodeProvider());

        /// <summary>
        /// Returns a C#-style type name (e.g. "string", "int", "object[]",
        /// "System.Collections.Hashtable") using the built-in CSharpCodeProvider
        /// so built-in primitives and arrays get their keyword form automatically.
        /// Falls back to the full .NET name if code generation fails.
        /// </summary>
        private static string FormatCSharpTypeName(Type type)
        {
            if (type == null) return string.Empty;
            try
            {
                return CSharpTypeFormatter.Value.GetTypeOutput(new System.CodeDom.CodeTypeReference(type));
            }
            catch
            {
                return type.FullName ?? type.Name;
            }
        }

        /// <summary>
        /// A single entry in the Variables panel.
        /// Category is "user" for user-created variables and "builtin" for
        /// SPE/Sitecore convenience variables set up during session init.
        /// Expandable indicates the variable has children (object properties,
        /// collection items, hashtable entries) worth showing on demand.
        /// </summary>
        public class VariableEntry
        {
            public string Name { get; set; }
            public string TypeName { get; set; }
            public string Preview { get; set; }
            public string Category { get; set; }
            public bool Expandable { get; set; }
        }

        /// <summary>
        /// Enumerates session variables visible in the ISE Variables panel.
        /// Splits results into "user" (user-created) and "builtin" (SPE/Sitecore
        /// convenience variables). PowerShell engine automatic variables are
        /// filtered out entirely.
        /// </summary>
        public List<VariableEntry> GetUserVariables()
        {
            var result = new List<VariableEntry>();
            lock (this)
            {
                try
                {
                    // Enumerate via the Variable: PSDrive through the session's own
                    // provider intrinsics - this walks the session's actual variable
                    // scope (same one GetDebugVariable reads from by name), unlike
                    // Engine.InvokeCommand.InvokeScript which would create a child
                    // scope that can't see user variables at the top-level script scope.
                    var items = Engine.SessionState.InvokeProvider.Item.Get(@"Variable:\*");
                    if (items == null)
                    {
                        PowerShellLog.Debug("[Session] action=getUserVariables Variable: provider returned null");
                        return result;
                    }

                    int totalFound = 0;
                    int filtered = 0;
                    foreach (var item in items)
                    {
                        totalFound++;
                        var psVar = item?.BaseObject as PSVariable;
                        if (psVar == null)
                        {
                            filtered++;
                            continue;
                        }
                        var name = psVar.Name;
                        if (string.IsNullOrEmpty(name) || EngineAutomaticVariableNames.Contains(name))
                        {
                            filtered++;
                            continue;
                        }
                        if ((psVar.Options & (ScopedItemOptions.Constant | ScopedItemOptions.ReadOnly)) != 0)
                        {
                            filtered++;
                            continue;
                        }

                        // Unwrap PSObject layers the same way the inline tooltip does
                        // (see PowerShellWebService.GetVariableValue) so we expose the
                        // underlying .NET type (e.g. Sitecore.Data.Items.Item) rather
                        // than System.Management.Automation.PSObject, and so the
                        // VariableDetails formatter sees the real object.
                        var variable = psVar.Value.BaseObject();
                        if (variable is PSCustomObject)
                        {
                            variable = psVar.Value;
                        }
                        var typeName = variable == null ? "$null" : FormatCSharpTypeName(variable.GetType());
                        string preview;
                        bool expandable = false;
                        try
                        {
                            var details = new VariableDetails("$" + name, variable);
                            preview = details.ValueString ?? string.Empty;
                            expandable = details.IsExpandable;
                        }
                        catch (Exception ex)
                        {
                            preview = "<" + ex.Message + ">";
                        }
                        result.Add(new VariableEntry
                        {
                            Name = name,
                            TypeName = typeName,
                            Preview = preview ?? string.Empty,
                            Category = SpeBuiltInVariableNames.Contains(name) ? "builtin" : "user",
                            Expandable = expandable
                        });
                    }
                    PowerShellLog.Debug($"[Session] action=getUserVariables total={totalFound} filtered={filtered} returned={result.Count}");
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"[Session] action=getUserVariables failed: {ex.Message}", ex);
                }
            }
            return result.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public void SetBreakpoints(IEnumerable<int> breakpoints)
        {
            lock (this)
            {
                Breakpoints = breakpoints.ToArray();
            }
        }

        public void InitBreakpoints()
        {
            lock (this)
            {
                var debugging = Debugging;
                Debugging = false;
                var breakpointsScript = Breakpoints.Aggregate(string.Empty,
                    (current, breakpoint) =>
                        current + $"Set-PSBreakpoint -Script {DebugFile} -Line {breakpoint + 1}\n");
                ExecuteScriptPart(breakpointsScript, false, true, false);
                Breakpoints = new int[0];
                Debugging = debugging;
            }
        }

        private void DebuggerOnBreakpointUpdated(object sender, BreakpointUpdatedEventArgs args)
        {
            if (Interactive)
            {
                if (args.Breakpoint is LineBreakpoint breakpoint)
                {
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
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            if (job == null) return;

            var sheerMessage = new SendMessageMessage(message, false);
            message.Arguments.Add("JobId", Key);
            job.MessageQueue.PutMessage(sheerMessage);
        }

        private void DebuggerOnDebuggerStop(object sender, DebuggerStopEventArgs args)
        {
            Debugger debugger = sender as Debugger;
            DebuggerResumeAction? resumeAction = null;
            DebuggingInBreakpoint = true;
            try
            {
                if (
                    ((ScriptingHostUserInterface) host.UI).CheckSessionCanDoInteractiveAction(
                        nameof(DebuggerOnDebuggerStop),false))
                {

                    var output = new PSDataCollection<PSObject>();
                    output.DataAdded += (dSender, dArgs) =>
                    {
                        foreach (var item in output.ReadAll())
                        {
                            host.UI.WriteLine(item.ToString());
                        }
                    };

                    var message = Message.Parse(this, "ise:breakpointhit");
                    //var position = args.InvocationInfo.DisplayScriptPosition;
                    IScriptExtent position;
                    try
                    {
                        position = args.InvocationInfo.GetType()
                            .GetProperty("ScriptPosition",
                                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty)
                            ?.GetValue(args.InvocationInfo) as IScriptExtent;
                    }
                    catch (Exception)
                    {
                        position = args.InvocationInfo.DisplayScriptPosition;
                    }

                    if (position != null)
                    {
                        message.Arguments.Add("Line", (position.StartLineNumber - 1).ToString());
                        message.Arguments.Add("Column", (position.StartColumnNumber - 1).ToString());
                        message.Arguments.Add("EndLine", (position.EndLineNumber - 1).ToString());
                        message.Arguments.Add("EndColumn", (position.EndColumnNumber - 1).ToString());
                    }
                    else
                    {
                        message.Arguments.Add("Line", (args.InvocationInfo.ScriptLineNumber - 1).ToString());
                        message.Arguments.Add("Column", (args.InvocationInfo.OffsetInLine - 1).ToString());
                        message.Arguments.Add("EndLine", (args.InvocationInfo.ScriptLineNumber).ToString());
                        message.Arguments.Add("EndColumn", (0).ToString());

                    }

                    message.Arguments.Add("HitCount",
                        args.Breakpoints.Count > 0 ? args.Breakpoints[0].HitCount.ToString() : "1");
                    SendUiMessage(message);

                    while (resumeAction == null && !abortRequested)
                    {
                        if (ImmediateCommand is string commandString)
                        {
                            PowerShellLog.Debug($"[Session] action=debugCommand session={Key}");
                            PowerShellLog.Debug(commandString);
                            DebuggerCommandResults results = null;
                            try
                            {
                                var psCommand = new PSCommand();
                                var scriptCommand = new Command(commandString, true)
                                {
                                    MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Warning
                                };
                                psCommand.AddCommand(scriptCommand)
                                    .AddCommand(OutDefaultCommand);

                                results = debugger?.ProcessCommand(psCommand, output);
                                ImmediateResults = output;
                                LogErrors(null, output.ToList());
                            }
                            catch (Exception ex)
                            {
                                PowerShellLog.Error("[Session] action=debugCommandFailed", ex);
                                ImmediateCommand = null;
                                host.UI.WriteErrorLine(GetExceptionString(ex,ExceptionStringFormat.Default));
                            }

                            if (results?.ResumeAction != null)
                            {
                                resumeAction = results.ResumeAction;
                            }
                            ImmediateCommand = null;
                        }
                        else
                        {
                            Thread.Sleep(20);
                        }
                    }


                    args.ResumeAction = resumeAction ?? DebuggerResumeAction.Continue;
                }
            }
            finally
            {
                DebuggingInBreakpoint = false;
            }
        }

        public bool TryInvokeInRunningSession(string script, bool stringOutput = false)
            => TryInvokeInRunningSessionInternal(script, out List<object> results, stringOutput);


        public bool TryInvokeInRunningSession(string script, out List<object> results, bool stringOutput = false)
            => TryInvokeInRunningSessionInternal(script, out results, stringOutput);


        public bool TryInvokeInRunningSession(Command command, bool stringOutput = false)
            => TryInvokeInRunningSessionInternal(command, out List<object> results, stringOutput);

        public bool TryInvokeInRunningSession(Command command, out List<object> results, bool stringOutput = false)
            => TryInvokeInRunningSessionInternal(command, out results, stringOutput);
        

        private bool TryInvokeInRunningSessionInternal(object executable, out List<object> results, bool stringOutput)
        {
            if (Debugging || host.NestedLevel > 0)
            {
                ImmediateCommand = executable;
                var tries = 20;
                Thread.Sleep(40);
                while (ImmediateCommand != null && tries > 0)
                {
                    Thread.Sleep(100);
                    tries--;
                }
                results = ImmediateResults.BaseList<object>();
                ImmediateResults = null;
                return tries > 0;
            }

            var command = executable as Command;
                results = command != null
                    ? InvokeInNewPowerShell(command, OutTarget.OutNone).BaseList<object>()
                    : ExecuteScriptPart(executable as string, stringOutput, true, true);
            return true;
        }

        public enum OutTarget
        {
            OutDefault,
            OutHost,
            OutNull,
            OutNone
        }

        private Collection<PSObject> InvokeInNewPowerShell(Command command, OutTarget target)
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
                LogErrors(ps, results);
                return results;
            }
        }

        internal object ImmediateCommand { get; set; }
        internal object ImmediateResults { get; set; }

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

        private void Initialize(bool reinitialize)
        {
            lock (this)
            {
                if (initialized && !reinitialize) return;

                initialized = true;
                
                UserName = User.Current.Name;
                var proxy = host.Runspace.SessionStateProxy;
                proxy.SetVariable("me", UserName);
                proxy.SetVariable("HostSettings", Settings);
                proxy.SetVariable("ScriptSession", this);

                var serverAuthority = HttpContext.Current?.Request?.Url?.GetLeftPart(UriPartial.Authority);
                if (!string.IsNullOrEmpty(serverAuthority))
                {
                    proxy.SetVariable("SitecoreAuthority", serverAuthority);
                }

                ExecuteScriptPart(RenamedCommands.AliasSetupScript, false, true, false);

                if (GetVariable("PSVersionTable") is Hashtable psVersionTable)
                {
                    psVersionTable["SPEVersion"] = CurrentVersion.SpeVersion;
                    psVersionTable["SitecoreVersion"] = SitecoreVersion.Display;
                }
            }
        }

        public ScriptBlock GetScriptBlock(string scriptBlock)
        {
            return host.Runspace.SessionStateProxy.InvokeCommand.NewScriptBlock(scriptBlock);
        }

        public List<object> ExecuteScriptPart(string script)
        {
            PrivateData.DeferredMessages.Clear();
            return ExecuteScriptPart(script, true, false);
        }

        public List<object> ExecuteScriptPart(Item scriptItem, bool stringOutput)
        {
            if (!scriptItem.IsPowerShellScript())
            {
                return new List<object>();
            }
            SetExecutedScript(scriptItem);
            var script = scriptItem[Templates.Script.Fields.ScriptBody] ?? string.Empty;
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

            if (!internalScript)
            {
                PowerShellLog.Debug($"[Session] action=scriptExecuting session={Key}");
                PowerShellLog.Debug(script);
            }

            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            return SpeTimer.Measure($"script execution in ScriptSession '{Key}'", !internalScript, () =>
            {
                var _pipeline = powerShell;
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
                    powerShell = _pipeline;
                }
            });
        }

        internal List<object> ExecuteCommand(Command command, bool stringOutput, bool internalScript)
        {
            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            var _pipeline = powerShell;
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
                powerShell = _pipeline;
            }
        }

        public void Abort()
        {
            if (DebuggingInBreakpoint)
            {
                TryInvokeInRunningSession("quit");
            }
            PowerShellLog.Info($"[Session] action=scriptAborting session={Key}");
            try
            {
                powerShell?.Stop();
            }
            catch (Exception ex)
            {
                PowerShellLog.Error($"[Session] action=scriptAbortFailed session={Key}", ex);
            }
            abortRequested = true;
        }

        private static Command OutDefaultCommand
            => new Command("Out-Default")
            {
                MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error
            };

        /// <summary>
        /// command for formatted output of everything.
        /// </summary>
        /// <remarks>
        /// "Out-Default" is not suitable for external apps, output goes to console.
        /// </remarks>
        public static Command OutHostCommand
            => new Command("Out-Host")
            {
                MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error
            };

        /// <summary>
        /// command for formatted output of everything.
        /// </summary>
        /// <remarks>
        /// "Out-Default" is not suitable for external apps, output goes to console.
        /// </remarks>
        private static Command OutNullCommand
            => new Command("Out-Null")
            {
                MergeUnclaimedPreviousCommandResults = PipelineResultTypes.Output | PipelineResultTypes.Error
            };

        
        private List<object> ExecuteCommand(bool stringOutput, bool marshallResults = true)
        {
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();
            JobName = job?.Name;

            if (stringOutput)
            {
                powerShell.Commands.AddCommand(OutDefaultCommand);
            }

            if (Runspace.DefaultRunspace == null)
            {
                Runspace.DefaultRunspace = host.Runspace;
            }

            if (Debugging)
            {
                host.Runspace.Debugger.DebuggerStop += DebuggerOnDebuggerStop;
                host.Runspace.Debugger.BreakpointUpdated += DebuggerOnBreakpointUpdated;
                SetVariable("SpeDebug", true);
                if (Interactive)
                {
                    var message = Message.Parse(this, "ise:debugstart");
                    SendUiMessage(message);
                }
            }
            else
            {
                Engine.SessionState.PSVariable.Remove("SpeDebug");
            }
            abortRequested = false;

            LastErrors?.Clear();
            // execute the commands in the pipeline now
            var execResults = powerShell.Invoke();
            if (powerShell.HadErrors)
            {
                LastErrors = powerShell.Streams.Error.ToList();
            }

            LogErrors(powerShell, execResults);

            if (Interactive && Debugging)
            {
                host.Runspace.Debugger.DebuggerStop -= DebuggerOnDebuggerStop;
                host.Runspace.Debugger.BreakpointUpdated -= DebuggerOnBreakpointUpdated;

                var message = Message.Parse(this, "ise:debugend");
                SendUiMessage(message);
                Debugging = false;
            }

            JobName = string.Empty;
            if(Runspace.DefaultRunspace == host.Runspace)
                Runspace.DefaultRunspace = null;
            return marshallResults
                ? execResults?.Select(p => p?.BaseObject).ToList()
                : execResults?.Cast<object>().ToList();
        }

        private static void LogErrors(System.Management.Automation.PowerShell powerShell, IEnumerable<PSObject> execResults)
        {
            if (powerShell?.HadErrors ?? false)
            {
                var errors = powerShell.Streams.Error.ToList();
                foreach (var record in errors)
                {
                    PowerShellLog.Warn(record + record.InvocationInfo.PositionMessage, record.Exception);
                }
            }

            if (execResults != null && execResults.Any())
            {
                foreach (var record in execResults.Where(r => r != null).Select(p => p.BaseObject).OfType<ErrorRecord>())
                {
                    PowerShellLog.Warn(record + record.InvocationInfo.PositionMessage, record.Exception);
                }
            }
        }

        public static string GetExceptionString(Exception ex, ExceptionStringFormat format = ExceptionStringFormat.Default)
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
            if (item == null) return;

            var psObject = ItemShellExtensions.GetPsObject(Engine.SessionState, item);
            SetVariable("SitecoreContextItem", psObject);

            var path = item.GetProviderPath();
            if (path.Contains(PathUtilities.OrphanPath))
            {
                PowerShellLog.Error($"[Session] action=orphanItem path=\"{path}\" item={item.ID}");
                path = $"{item.Database}:\\";
            }
            
            Engine.SessionState.Path.SetLocation(path);
        }

        public void SetItemContextFromLocation()
        {
            var provider = Engine.SessionState.Path.CurrentLocation.Provider;

            if (provider.ImplementingType == typeof(PsSitecoreItemProvider) ||
                provider.ImplementingType.IsSubclassOf(typeof(PsSitecoreItemProvider)))
            {
                var path = Engine.SessionState.Path.CurrentLocation.ProviderPath;

                var colonIndex = path.IndexOf(':');
                var relativePath = path.Substring(colonIndex + 1).Replace('\\', '/');
                var databaseName = path.Substring(0, colonIndex);

                var item = PathUtilities.GetItem(databaseName, relativePath);

                if (item != null)
                {
                    var psObject = ItemShellExtensions.GetPsObject(Engine.SessionState, item);
                    SetVariable("SitecoreContextItem", psObject);
                    return;
                }
            }

            // This is either not a Sitecore drive, or no item could be determined from the path, so remove it.
            RemoveVariable("SitecoreContextItem");
        }

        public void SetExecutedScript(Item scriptItem)
        {
            if (scriptItem == null) return;

            var scriptPath = scriptItem.GetProviderPath();
            PowerShellLog.Debug($"[Session] action=scriptItemSet path=\"{scriptPath}\" session={Key}");
            SetVariable("SitecoreScriptRoot", scriptItem.Parent.GetProviderPath());
            SetVariable("SitecoreCommandPath", scriptPath);
            SetVariable("PSScript", scriptItem);
        }

        #region IDisposable logic

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Close()
        {
            if(Runspace.DefaultRunspace == host.Runspace)
                Runspace.DefaultRunspace = null;
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