using System;
using System.Threading;
using Cognifide.PowerShell.Client.Controls;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Jobs.AsyncUI;

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
                Context.Language = Context.Job?.Options?.ClientLanguage ?? Context.Language;
                Method(Session, Script);
                if (Context.Job == null) return;

                var output = new RunnerOutput
                {
                    Exception = null,
                    Output = Session.Output.GetHtmlUpdate(),
                    HasErrors = Session.Output.HasErrors,
                    CloseRunner = Session.CloseRunner
                };

                Context.Job.Status.Result = output;
                var message = new CompleteMessage {RunnerOutput = output};
                JobContext.MessageQueue.PutMessage(message);
            }
            catch (ThreadAbortException taex)
            {
                PowerShellLog.Error("Script was aborted", taex);
                if (!Environment.HasShutdownStarted)
                {
                    Thread.ResetAbort();
                }
            }
            catch (Exception exc)
            {
                PowerShellLog.Error("Error while executing PowerShell script.", exc);

                if (Context.Job != null)
                {
                    var output =  new RunnerOutput
                    {
                        Exception = exc,
                        Output = Session.Output.GetHtmlUpdate(),
                        HasErrors = true,
                        CloseRunner = Session.CloseRunner
                    };

                    Context.Job.Status.Result = output;
                    var message = new CompleteMessage { RunnerOutput = output };
                    JobContext.MessageQueue.PutMessage(message);
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