using System;
using System.Threading;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class ScriptRunner
    {
        private readonly ProgressBoxMethod method;
        private readonly object[] parameters;

        public ScriptRunner(ProgressBoxMethod method, object[] parameters)
        {
            Assert.ArgumentNotNull(method, "method");
            Assert.ArgumentNotNull(parameters, "parameters");
            this.method = method;
            this.parameters = parameters;
        }

        public ProgressBoxMethod Method
        {
            get { return method; }
        }

        public object[] Parameters
        {
            get { return parameters; }
        }

        public void Run()
        {
            try
            {
                Method(Parameters);
            }
            catch (ThreadAbortException)
            {
                if (!Environment.HasShutdownStarted)
                    Thread.ResetAbort();
                Log.Info("Script was aborted", this);
                JobContext.PostMessage("ise:updateresult");
                JobContext.Flush();
            }
            catch (Exception ex)
            {
                Log.Error("Script failed: " + ex, this);
                JobContext.Job.Status.Result = ex;
                JobContext.PostMessage("ise:updateresult");
                JobContext.Flush();
            }
        }
    }
}