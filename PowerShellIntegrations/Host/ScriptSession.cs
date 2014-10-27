using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Provider;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class ScriptSession : IDisposable
    {
        private const string HtmlExceptionFormatString =
            "<div class='white-space:normal; width:70%;'>{1}</div>{0}<strong>Of type</strong>: {3}{0}<strong>Stack trace</strong>:{0}{2}{0}";

        private const string HtmlInnerExceptionPrefix = "{0}<strong>Inner Exception:</strong>{1}";
        private const string LineEndformat = "\n";

        private const string ConsoleExceptionFormatString =
            "[[;#f00;#000]Exception: {0}]\r\n" + "[[;#f00;#000]Type: {1}]\r\n" + "[[;#f00;#000]Stack trace:\r\n{2}]";

        private const string ConsoleInnerExceptionPrefix = "\r\n[[b;#fff;#F00]Inner ] {0}";
        private readonly ScriptingHost host;
        private readonly Runspace runspace;

        private bool disposed;
        private bool initialized;
        private Pipeline pipeline;

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

        public string Key { get; internal set; }

        public ScriptSession(string applianceType) : this(applianceType, true)
        {
        }

        public void SetContextItem(Item item)
        {
            SetVariable("SitecoreContextItem", item);
        }

        public ScriptSession(string applianceType, bool personalizedSettings)
        {
            // Create and open a PowerShell runspace.  A runspace is a container 
            // that holds PowerShell pipelines, and provides access to the state
            // of the Runspace session (aka SessionState.)
            ApplianceType = applianceType;
            Settings = ApplicationSettings.GetInstance(ApplianceType, personalizedSettings);

            RunspaceConfiguration conf = RunspaceConfiguration.Create();
            host = new ScriptingHost(Settings, conf);
            runspace = host.Runspace;

            conf.Cmdlets.Append(CognifideSitecorePowerShellSnapIn.Commandlets);
            if (Settings.UseTypeInfo)
            {
                conf.Formats.Prepend(
                    new FormatConfigurationEntry(HttpRuntime.AppDomainAppPath + @"Console\Assets\Sitecore.Views.ps1xml"));
                conf.Formats.Update();
                conf.Types.Prepend(
                    new TypeConfigurationEntry(HttpRuntime.AppDomainAppPath + @"Console\Assets\Sitecore.Types.ps1xml"));
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

        public ApplicationSettings Settings { get; private set; }

        public OutputBuffer Output { get { return host.Output; } }

        public string CurrentLocation { get { return runspace.SessionStateProxy.Path.CurrentLocation.Path; } }

        public string ApplianceType { get; set; }

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
                runspace.SessionStateProxy.SetVariable("AppPath", HttpRuntime.AppDomainAppPath);
                runspace.SessionStateProxy.SetVariable("AppVPath", HttpRuntime.AppDomainAppVirtualPath);
                runspace.SessionStateProxy.SetVariable("tempPath", Environment.GetEnvironmentVariable("temp"));
                runspace.SessionStateProxy.SetVariable("tmpPath", Environment.GetEnvironmentVariable("tmp"));
                runspace.SessionStateProxy.SetVariable("me", User.Current.Name);
                runspace.SessionStateProxy.SetVariable("HttpContext", HttpContext.Current);
                if (HttpContext.Current != null)
                {
                    runspace.SessionStateProxy.SetVariable("request", HttpContext.Current.Request);
                    runspace.SessionStateProxy.SetVariable("response", HttpContext.Current.Response);
                }
                runspace.SessionStateProxy.SetVariable("ClientData", Context.ClientData);
                runspace.SessionStateProxy.SetVariable("SitecoreDataFolder",
                    Sitecore.Configuration.Settings.DataFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreDebugFolder",
                    Sitecore.Configuration.Settings.DebugFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreIndexFolder",
                    Sitecore.Configuration.Settings.IndexFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreLayoutFolder",
                    Sitecore.Configuration.Settings.LayoutFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreLogFolder",
                    Sitecore.Configuration.Settings.LogFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreMediaFolder",
                    Sitecore.Configuration.Settings.MediaFolder);
                runspace.SessionStateProxy.SetVariable("SitecorePackageFolder",
                    Sitecore.Configuration.Settings.PackagePath);
                runspace.SessionStateProxy.SetVariable("SitecoreSerializationFolder",
                    Sitecore.Configuration.Settings.SerializationFolder);
                runspace.SessionStateProxy.SetVariable("SitecoreTempFolder",
                    Sitecore.Configuration.Settings.TempFolderPath);

                try
                {
                    runspace.SessionStateProxy.SetVariable("ClientPage", Context.ClientPage);
                }
                catch
                {
                    Log.Warn("Unable to set the ClientPage variable.", this);
                }
                runspace.SessionStateProxy.SetVariable("HostSettings", Settings);
                runspace.SessionStateProxy.SetVariable("ScriptSession", this);
                if (PsVersion == null)
                {
                    PsVersion = (Version) ExecuteScriptPart("$PSVersionTable.PSVersion", false, true)[0];
                }
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
            if (String.IsNullOrEmpty(script))
            {
                return null;
            }

            Log.Info("Executing script:\n" + script, this);

            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            pipeline = runspace.CreatePipeline(script);
            return ExecuteCommand(stringOutput, internalScript, pipeline, marshalResults);
        }

        internal List<object> ExecuteCommand(Command command, bool stringOutput, bool internalScript)
        {
            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            pipeline = runspace.CreatePipeline();

            pipeline.Commands.Add(command);

            return ExecuteCommand(stringOutput, internalScript, pipeline);
        }

        public void Abort()
        {
            pipeline.Stop();
        }

        private List<object> ExecuteCommand(bool stringOutput, bool internalScript, Pipeline pipeline,
            bool marshallResults = true)
        {
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

            //Output = host.Output;
            return marshallResults ? execResults.Select(p => p.BaseObject).ToList() : execResults.Cast<object>().ToList();
        }

        private void PipelineStateChanged(object sender, PipelineStateEventArgs e)
        {
        }

        public string GetExceptionString(Exception exc)
        {
            var exception = String.Empty;
            exception += String.Format(
                HtmlExceptionFormatString,
                LineEndformat,
                exc.Message,
                exc.StackTrace.Replace("\n", LineEndformat),
                exc.GetType());
            if (exc.InnerException != null)
            {
                exception += String.Format(HtmlInnerExceptionPrefix, LineEndformat,
                    GetExceptionString(exc.InnerException));
            }
            return exception;
        }

        public string GetExceptionConsoleString(Exception exc)
        {
            var exception = String.Empty;
            exception += String.Format(
                ConsoleExceptionFormatString,
                exc.Message,
                exc.GetType(),
                exc.StackTrace.Replace("[", "%((%").Replace("]", "%))%"));
            if (exc.InnerException != null)
            {
                exception += String.Format(ConsoleInnerExceptionPrefix, GetExceptionConsoleString(exc.InnerException));
            }
            return exception;
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

        public void SetItemLocationContext(Item item)
        {
            SetContextItem(item);
            var contextScript = GetDataContextSwitch(item);
            ExecuteScriptPart(contextScript);
        }
    }
}