﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Threading;
using System.Web;
using System.Xml;
using Cognifide.PowerShell.PowerShellIntegrations.Provider;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
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
        private static readonly List<CmdletConfigurationEntry> commandlets = new List<CmdletConfigurationEntry>();
        private readonly ScriptingHost host;
        private readonly Runspace runspace;

        private bool disposed;
        private bool initialized;

        static ScriptSession()
        {
            XmlNodeList cmdltsToIncludes = Factory.GetConfigNodes("powershell/commandlets/add");
            foreach (XmlElement cmdltToInclude in cmdltsToIncludes)
            {
                string[] cmdltTypeDef = cmdltToInclude.Attributes["type"].Value.Split(',');
                string cmdletType = cmdltTypeDef[0];
                string cmdletAssembly = cmdltTypeDef[1];
                WildcardPattern wildcard = GetWildcardPattern(cmdletType);
                Assembly assembly = Assembly.Load(cmdletAssembly);
                GetCommandletsFromAssembly(assembly, wildcard);
            }
        }

        public ScriptSession(string applianceType) : this(applianceType, true)
        {
        }

        public ScriptSession(string applianceType, bool personalizedSettings)
        {
            // Create and open a PowerShell runspace.  A runspace is a container 
            // that holds PowerShell pipelines, and provides access to the state
            // of the Runspace session (aka SessionState.)
            ApplianceType = applianceType;
            Settings = ApplicationSettings.GetInstance(ApplianceType, personalizedSettings);
            RunspaceConfiguration conf = RunspaceConfiguration.Create();
            host = new ScriptingHost(Settings);
            host.UI.RawUI.BufferSize = new Size(Settings.HostWidth, Int32.MaxValue);
            InitialSessionState initState = InitialSessionState.CreateDefault();
            initState.ThreadOptions = PSThreadOptions.UseCurrentThread;
            initState.ApartmentState = ApartmentState.STA;
            runspace = RunspaceFactory.CreateRunspace(host, conf);
            conf.Cmdlets.Append(commandlets);
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
        }

        public ApplicationSettings Settings { get; private set; }

        public OutputBuffer Output
        {
            get { return host.Output; }
        }

        public string CurrentLocation
        {
            get { return runspace.SessionStateProxy.Path.CurrentLocation.Path; }
        }

        public string ApplianceType { get; set; }

        protected static WildcardPattern GetWildcardPattern(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                name = "*";
            }
            const WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
            var wildcard = new WildcardPattern(name, options);
            return wildcard;
        }

        private static void GetCommandletsFromAssembly(Assembly assembly, WildcardPattern wildcard)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.GetCustomAttributes(typeof (CmdletAttribute), true).Length > 0 &&
                    wildcard.IsMatch(type.FullName))
                {
                    var attribute = (CmdletAttribute) (type.GetCustomAttributes(typeof (CmdletAttribute), true)[0]);
                    commandlets.Add(new CmdletConfigurationEntry(attribute.VerbName + "-" + attribute.NounName, type, ""));
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

        public void Initialize()
        {
            Initialize(false);
        }

        public void Initialize(bool reinitialize)
        {
            lock (this)
            {
                if (!initialized || reinitialize)
                {
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
                    try
                    {
                        runspace.SessionStateProxy.SetVariable("ClientPage", Context.ClientPage);
                    }
                    catch
                    {
                    }
                    runspace.SessionStateProxy.SetVariable("HostSettings", Settings);
                }
            }
        }

        public static string GetDataContextSwitch(Item item)
        {
            return String.Format(
                "cd \"{0}:{1}\"\n", item.Database.Name,
                item.Paths.Path.Replace("/", "\\").Substring(9));
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
            if (String.IsNullOrEmpty(script))
            {
                return null;
            }

            Log.Info("Executing script:\n" + script, this);

            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            Pipeline pipeline = runspace.CreatePipeline(script);

            return ExecuteCommand(stringOutput, internalScript, pipeline);
        }

        internal List<object> ExecuteCommand(Command command, bool stringOutput, bool internalScript)
        {
            // Create a pipeline, and populate it with the script given in the
            // edit box of the form.
            Pipeline pipeline = runspace.CreatePipeline();

            pipeline.Commands.Add(command);

            return ExecuteCommand(stringOutput, internalScript, pipeline);
        }

        private List<object> ExecuteCommand(bool stringOutput, bool internalScript, Pipeline pipeline)
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
            Collection<PSObject> execResults = pipeline.Invoke();

            //Output = host.Output;

            List<object> results = execResults.Select(p => p.BaseObject).ToList();

            return results;
        }

        private void PipelineStateChanged(object sender, PipelineStateEventArgs e)
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    runspace.Dispose();
                }

                // There are no unmanaged resources to release, but
                // if we add them, they need to be released here.
            }
            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            //base.Dispose(disposing);
        }

        public string GetExceptionString(Exception exc)
        {
            string exception = String.Empty;
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
            string exception = String.Empty;
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
        }

        #endregion

        public void SetItemLocationContext(Item item)
        {
            string contextScript = ScriptSession.GetDataContextSwitch(item);
            ExecuteScriptPart(contextScript);
        }

    }
}