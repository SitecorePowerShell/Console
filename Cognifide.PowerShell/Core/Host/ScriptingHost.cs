﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Threading;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;

namespace Cognifide.PowerShell.Core.Host
{
    public class ScriptingHost : PSHost
    {
        private readonly ScriptingHostPrivateData privateData;
        private readonly ScriptingHostUserInterface ui;
        private readonly InitialSessionState sessionState;
        private Runspace runspace;
        public int NestedLevel { get; private set; }
        public int UiNestedLevel { get; private set; }

        /// <summary>
        ///     Initializes a new instance of the MyHost class. Keep
        ///     a reference to the host application object so that it
        ///     can be informed of when to exit.
        /// </summary>
        public ScriptingHost(ApplicationSettings settings, InitialSessionState initialState)
        {
            ui = new ScriptingHostUserInterface(settings, this);
            privateData = new ScriptingHostPrivateData(this);
            sessionState = initialState;
            CloseRunner = false;
            CloseMessages = new List<string>();
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        public OutputBuffer Output => ui.Output;

        /// <summary>
        ///     Return the culture information to use. This implementation
        ///     returns a snapshot of the culture information of the thread
        ///     that created this object.
        /// </summary>
        public override CultureInfo CurrentCulture { get; } = Thread.CurrentThread.CurrentCulture;

        /// <summary>
        ///     Return the UI culture information to use. This implementation
        ///     returns a snapshot of the UI culture information of the thread
        ///     that created this object.
        /// </summary>
        public override CultureInfo CurrentUICulture { get; } = Thread.CurrentThread.CurrentUICulture;

        /// <summary>
        ///     This implementation always returns the GUID allocated at
        ///     instantiation time.
        /// </summary>
        public override Guid InstanceId { get; } = Guid.NewGuid();

        /// <summary>
        ///     This implementation always returns the GUID allocated at
        ///     instantiation time.
        /// </summary>
        public string SessionId { get; internal set; }
        public string SessionKey { get; internal set; }

        public bool CloseRunner { get; internal set; }
        public List<string> CloseMessages { get; internal set; }
        public string User { get; internal set; }
        public string JobName { get; internal set; }
        public bool Interactive { get; internal set; }

        /// <summary>
        ///     This implementation always returns the GUID allocated at
        ///     instantiation time.
        /// </summary>
        public bool AutoDispose { get; internal set; }

        /// <summary>
        ///     Return a string that contains the name of the host implementation.
        ///     Keep in mind that this string may be used by script writers to
        ///     identify when your host is being used.
        /// </summary>
        public override string Name => ScriptSession.PsVersion == null
            ? "Sitecore PowerShell Extensions Host"
            : $"Sitecore PowerShell Extensions Host {CurrentVersion.SpeVersion} on Windows PowerShell {ScriptSession.PsVersion.Major}.{ScriptSession.PsVersion.Minor} & Sitecore {SitecoreVersion.Current.Major}.{SitecoreVersion.Current.Minor}";

        /// <summary>
        ///     This sample does not implement a PSHostUserInterface component so
        ///     this property simply returns null.
        /// </summary>
        public override PSHostUserInterface UI => ui;

        /// <summary>
        ///     Return the version object for this application. Typically this
        ///     should match the version resource in the application.
        /// </summary>
        public override Version Version => CurrentVersion.SpeVersion;

        public override PSObject PrivateData => new PSObject(privateData);


        /// <summary>
        ///     Gets or sets the runspace used by the PSSession.
        /// </summary>
        public Runspace Runspace => runspace ?? (runspace = RunspaceFactory.CreateRunspace(this, sessionState));

        public void EndNestedPromptSuspension()
        {
            UiNestedLevel--;
        }
        /// <summary>
        ///     Not implemented by this example class. The call fails with
        ///     a NotImplementedException exception.
        /// </summary>
        public override void EnterNestedPrompt()
        {
            NestedLevel++;
            UiNestedLevel++;
            var resultSig = Guid.NewGuid().ToString();

            var str = new UrlString(UIUtil.GetUri("control:PowerShellConsole"));
            str.Add("sid", resultSig);
            str.Add("fc", privateData.ForegroundColor.ToString());
            str.Add("bc", privateData.BackgroundColor.ToString());
            str.Add("id", SessionKey);
            str.Add("suspend", "true");

            var currentNesting = NestedLevel;
            JobContext.MessageQueue.PutMessage(
                new ShowSuspendDialogMessage(SessionKey, str.ToString(), "900", "600", new Hashtable())
                {
                    ReceiveResults = true,
                });

            var scriptSession = ScriptSessionManager.GetSession(SessionKey);
            while (currentNesting <= UiNestedLevel)
            {
                if (currentNesting == UiNestedLevel)
                {
                    if (scriptSession.ImmediateCommand is string commandString)
                    {
                        scriptSession.ImmediateCommand = null;
                        PowerShellLog.Info($"Executing a command in ScriptSession '{SessionKey}'.");
                        PowerShellLog.Debug(commandString);
                        try
                        {
                            var result =
                                scriptSession.InvokeInNewPowerShell(commandString, ScriptSession.OutTarget.OutHost);
                        }
                        catch (Exception ex)
                        {
                            PowerShellLog.Error("Error while executing Debugging command.", ex);
                            UI.WriteErrorLine(ScriptSession.GetExceptionString(ex));
                        }
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }
            JobContext.MessageQueue.GetResult();

            ScriptSessionManager.GetSession(SessionKey).InvokeInNewPowerShell("exit", ScriptSession.OutTarget.OutHost);
        }

        /// <summary>
        ///     Not implemented by this example class. The call fails
        ///     with a NotImplementedException exception.
        /// </summary>
        public override void ExitNestedPrompt()
        {
            NestedLevel--;
        }

        /// <summary>
        ///     This API is called before an external application process is
        ///     started. Typically it is used to save state so the parent can
        ///     restore state that has been modified by a child process (after
        ///     the child exits). In this example, this functionality is not
        ///     needed so the method returns nothing.
        /// </summary>
        public override void NotifyBeginApplication()
        {
            //return;
        }

        /// <summary>
        ///     This API is called after an external application process finishes.
        ///     Typically it is used to restore state that a child process may
        ///     have altered. In this example, this functionality is not
        ///     needed so the method returns nothing.
        /// </summary>
        public override void NotifyEndApplication()
        {
            //return;
        }

        /// <summary>
        ///     Indicate to the host application that exit has
        ///     been requested. Pass the exit code that the host
        ///     application should use when exiting the process.
        /// </summary>
        /// <param name="exitCode">The exit code that the host application should use.</param>
        public override void SetShouldExit(int exitCode)
        {
        }
    }
}