using System;
using System.Linq;
using System.Text;
using System.Threading;
using Cognifide.PowerShell.Core.Host;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;

namespace Cognifide.PowerShell.Client.Applications
{
    public class ScriptRunner
    {
        public delegate void ScriptRunnerMethod(ScriptSession session, string script);

        private bool AutoDispose { get; set; }
        public ScriptRunnerMethod Method { get; private set; }
        public ScriptSession Session { get; private set; }
        public string Script { get; private set; }

        public ScriptRunner(ScriptRunnerMethod method, ScriptSession session, string script, bool autoDispose)
        {
            Assert.ArgumentNotNull(method, "method");
            Assert.ArgumentNotNull(session, "session");
            Assert.ArgumentNotNull(script, "script");
            Method = method;
            Session = session;
            Script = script;
            AutoDispose = autoDispose;
        }

        public void Run()
        {
            try
            {
                Method(Session, Script);
                if (Context.Job == null) return;

                Context.Job.Status.Result = new RunnerOutput
                {
                    Exception = null,
                    Output = Session.Output.GetHtmlUpdate(),
                    HasErrors = Session.Output.HasErrors,
                    CloseRunner = Session.CloseRunner
                };

                JobContext.PostMessage("psr:updateresults");
                JobContext.Flush();

            }
            catch (ThreadAbortException taex)
            {
                Log.Error("Script was aborted", taex, this);
                if (!Environment.HasShutdownStarted)
                {
                    Thread.ResetAbort();
                }
            }
            catch (Exception exc)
            {
                Log.Error(ScriptSession.GetExceptionString(exc), exc);

                if (Context.Job != null)
                {
                    Context.Job.Status.Result = new RunnerOutput
                    {
                        Exception = exc,
                        Output = Session.Output.GetHtmlUpdate(),
                        HasErrors = true,
                        CloseRunner = Session.CloseRunner
                    };

                    JobContext.PostMessage("psr:updateresults");
                    JobContext.Flush();
                }
            }
            finally
            {
                Session.CloseRunner = false;
                Session.Output.Clear();
                if (AutoDispose)
                {
                    Session.Dispose();
                }
            }
        }
    }
}