using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Web;
using Cognifide.PowerShell.Core.Provider;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Security.Accounts;
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

        private static readonly FormatConfigurationEntry formats = new FormatConfigurationEntry(HttpRuntime.AppDomainAppPath +
                                                   @"sitecore modules\PowerShell\Assets\Sitecore.Views.ps1xml");

        readonly TypeConfigurationEntry types = new TypeConfigurationEntry(HttpRuntime.AppDomainAppPath +
                                               @"sitecore modules\PowerShell\Assets\Sitecore.Types.ps1xml");

        private bool disposed;
        private bool initialized;
        private Pipeline pipeline;
        private readonly ScriptingHost host;
        private readonly Runspace runspace;

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

            var conf = RunspaceConfiguration.Create();
            host = new ScriptingHost(Settings, conf);
            runspace = host.Runspace;

            conf.Cmdlets.Append(CognifideSitecorePowerShellSnapIn.Commandlets);
            if (Settings.UseTypeInfo)
            {
                conf.Formats.Prepend(formats);
                conf.Formats.Update();
                conf.Types.Prepend(types);
                conf.Types.Update();
            }

            runspace.ThreadOptions = PSThreadOptions.UseCurrentThread;
            runspace.ApartmentState = ApartmentState.STA;
            PsSitecoreItemProvider.AppendToRunSpace(runspace.RunspaceConfiguration);
            runspace.Open();
            if (!initialized)
            {
                Initialize();
            }

            if (Settings.UseTypeInfo)
            {
                Output.Clear();
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
        public RunspaceAvailability State => runspace.RunspaceAvailability;
        public string ApplianceType { get; set; }

        public string CurrentLocation
        {
            get
            {
                if (runspace.RunspaceAvailability == RunspaceAvailability.Busy)
                {
                    return string.Empty;
                }
                try
                {
                    return runspace.SessionStateProxy.Path.CurrentLocation.Path;
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
                runspace.SessionStateProxy.SetVariable(varName, varValue);
            }
        }

        public object GetVariable(string varName)
        {
            lock (this)
            {
                return runspace.SessionStateProxy.GetVariable(varName);
            }
        }

        public void SetBreakpoints(IEnumerable<Breakpoint> breakpoints)
        {
            lock (this)
            {                
                runspace.Debugger.SetBreakpoints(breakpoints);
            }
        }

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
                var proxy = runspace.SessionStateProxy;
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

                foreach (var key in PredefinedVariables.Variables.Keys)
                {
                    proxy.SetVariable(key, PredefinedVariables.Variables[key]);
                }

                ExecuteScriptPart(RenamedCommands.AliasSetupScript, false, true, false);
            }
        }

        public ScriptBlock GetScriptBlock(string scriptBlock)
        {
            return runspace.SessionStateProxy.InvokeCommand.NewScriptBlock(scriptBlock);
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

            Log.Info("Executing a Sitecore PowerShell Extensions script.", this);
            Log.Debug(script, this);

            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            return SpeTimer.Measure("script execution", () =>
            {
                try
                {
                    pipeline = runspace.CreatePipeline(script);
                    return ExecuteCommand(stringOutput, internalScript, marshalResults);
                }
                finally
                {
                    pipeline.Dispose();
                    pipeline = null;
                }
            });
        }

        internal List<object> ExecuteCommand(Command command, bool stringOutput, bool internalScript)
        {
            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            try
            {
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.Add(command);
                return ExecuteCommand(stringOutput, internalScript);
            }
            finally
            {
                pipeline.Dispose();
                pipeline = null;
            }
        }

        public void Abort()
        {
            pipeline?.Stop();
        }

        private List<object> ExecuteCommand(bool stringOutput, bool internalScript, bool marshallResults = true)
        {
            JobName = Context.Job?.Name;
            if (!internalScript)
            {
                pipeline.Commands[0].MergeMyResults(PipelineResultTypes.Error, PipelineResultTypes.Output);
            }

            if (stringOutput)
            {
                pipeline.Commands.Add("out-default");
            }
            pipeline.StateChanged += PipelineStateChanged;

            // execute the commands in the pipeline now
            var execResults = pipeline.Invoke();

            if (execResults != null && execResults.Any())
            {
                foreach (
                    var record in
                        execResults.Select(p => p.BaseObject).OfType<ErrorRecord>().Select(result => result))
                {
                    Log.Error(record + record.InvocationInfo.PositionMessage, this);
                }
            }

            JobName = string.Empty;
            return marshallResults
                ? execResults.Select(p => p.BaseObject).ToList()
                : execResults.Cast<object>().ToList();
        }

        private void PipelineStateChanged(object sender, PipelineStateEventArgs e)
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
            runspace.Dispose();
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