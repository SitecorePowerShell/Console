using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management.Automation;
using System.Management.Automation.Host;
using System.Management.Automation.Runspaces;
using System.Threading;
using Cognifide.PowerShell.Core.Settings;

namespace Cognifide.PowerShell.Core.Host
{
    public class ScriptingHost : PSHost, IHostSupportsInteractiveSession
    {

        /// <summary>
        ///     The identifier of this PSHost implementation.
        /// </summary>
        private readonly Guid myId = Guid.NewGuid();

        private readonly Stack<Runspace> pushedRunspaces;
        private readonly RunspaceConfiguration runspaceConfiguration;
        private Runspace runspace;
        private readonly ScriptingHostPrivateData privateData;

        /// <summary>
        ///     The culture information of the thread that created
        ///     this object.
        /// </summary>
        private readonly CultureInfo originalCultureInfo =
            Thread.CurrentThread.CurrentCulture;

        /// <summary>
        ///     The UI culture information of the thread that created
        ///     this object.
        /// </summary>
        private readonly CultureInfo originalUiCultureInfo =
            Thread.CurrentThread.CurrentUICulture;

        private readonly ScriptingHostUserInterface ui;

        /// <summary>
        ///     Initializes a new instance of the MyHost class. Keep
        ///     a reference to the host application object so that it
        ///     can be informed of when to exit.
        /// </summary>
        public ScriptingHost(ApplicationSettings settings, RunspaceConfiguration runspaceConfiguration)
        {
            this.runspaceConfiguration = runspaceConfiguration;
            ui = new ScriptingHostUserInterface(settings);
            pushedRunspaces = new Stack<Runspace>();
            privateData = new ScriptingHostPrivateData(this);
            CloseRunner = false;
        }

        /// <summary>
        ///     A reference to the PSHost implementation.
        /// </summary>
        public OutputBuffer Output
        {
            get { return ui.Output; }
        }

        /// <summary>
        ///     Return the culture information to use. This implementation
        ///     returns a snapshot of the culture information of the thread
        ///     that created this object.
        /// </summary>
        public override CultureInfo CurrentCulture
        {
            get { return originalCultureInfo; }
        }

        /// <summary>
        ///     Return the UI culture information to use. This implementation
        ///     returns a snapshot of the UI culture information of the thread
        ///     that created this object.
        /// </summary>
        public override CultureInfo CurrentUICulture
        {
            get { return originalUiCultureInfo; }
        }

        /// <summary>
        ///     This implementation always returns the GUID allocated at
        ///     instantiation time.
        /// </summary>
        public override Guid InstanceId
        {
            get { return myId; }
        }

        /// <summary>
        ///     This implementation always returns the GUID allocated at
        ///     instantiation time.
        /// </summary>
        public string SessionId { get; internal set; }

        public bool CloseRunner { get; internal set; }

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
        public override string Name
        {
            get
            {
                if (ScriptSession.PsVersion == null)
                {
                    return "Sitecore Console Host";
                }
                return string.Format("Sitecore Console Host {0} on Windows PowerShell {1}.{2}",
                    GetType().Assembly.GetName().Version, ScriptSession.PsVersion.Major,
                    ScriptSession.PsVersion.Minor);
            }
        }

        /// <summary>
        ///     This sample does not implement a PSHostUserInterface component so
        ///     this property simply returns null.
        /// </summary>
        public override PSHostUserInterface UI
        {
            get { return ui; }
        }

        /// <summary>
        ///     Return the version object for this application. Typically this
        ///     should match the version resource in the application.
        /// </summary>
        public override Version Version
        {
            get { return GetType().Assembly.GetName().Version; }
        }

        public override PSObject PrivateData
        {
            get { return new PSObject(privateData); }
        }

        /// <summary>
        ///     Not implemented by this example class. The call fails with
        ///     a NotImplementedException exception.
        /// </summary>
        public override void EnterNestedPrompt()
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        /// <summary>
        ///     Not implemented by this example class. The call fails
        ///     with a NotImplementedException exception.
        /// </summary>
        public override void ExitNestedPrompt()
        {
            throw new NotImplementedException(
                "The method or operation is not implemented.");
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

        public void PushRunspace(Runspace runspace)
        {
            pushedRunspaces.Push(runspace);
        }

        public void PopRunspace()
        {
            pushedRunspaces.Pop();
        }

        /// <summary>
        ///     Gets a value indicating whether a request
        ///     to open a PSSession has been made.
        /// </summary>
        public bool IsRunspacePushed
        {
            get { return 0 < pushedRunspaces.Count; }
        }

        /// <summary>
        ///     Gets or sets the runspace used by the PSSession.
        /// </summary>
        public Runspace Runspace
        {
            get
            {
                if (null == runspace)
                {
                    runspace = RunspaceFactory.CreateRunspace(this, runspaceConfiguration);
                }

                Stack<Runspace> stack = pushedRunspaces;
                if (0 == stack.Count)
                {
                    return runspace;
                }

                return stack.Peek();
            }
        }
    }
}