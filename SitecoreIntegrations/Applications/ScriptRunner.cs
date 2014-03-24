using System;
using System.Linq;
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
        private readonly bool autoDispose;

        public ScriptRunner(ProgressBoxMethod method, object[] parameters, bool autoDispose)
        {
            Assert.ArgumentNotNull(method, "method");
            Assert.ArgumentNotNull(parameters, "parameters");
            this.method = method;
            this.parameters = parameters;
            this.autoDispose = autoDispose;
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
            catch (ThreadAbortException e)
            {
                Log.Error("Script was aborted", e, this);
                if (!Environment.HasShutdownStarted)
                {
                    Thread.ResetAbort();
                }
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
            finally
            {
                if (autoDispose)
                {
                    foreach (var parameter in Parameters.OfType<IDisposable>())
                    {
                        parameter.Dispose();
                    }
                }
            }
        }
    }
}