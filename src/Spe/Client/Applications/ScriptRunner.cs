using System;
using System.Threading;
using Sitecore;
using Sitecore.Diagnostics;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Client.Controls;
using Spe.Core.Diagnostics;
using Spe.Core.Host;
using Spe.Core.VersionDecoupling;

namespace Spe.Client.Applications
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
            var jobManager = TypeResolver.ResolveFromCache<IJobManager>();
            var job = jobManager.GetContextJob();

            try
            {
                Context.Language = job?.Options?.ClientLanguage ?? Context.Language;
                Method(Session, Script);
                if (job == null) return;

                var output = new RunnerOutput
                {
                    Exception = null,
                    Output = Session.Output.GetHtmlUpdate(),
                    DialogResult = Session.Output.GetDialogRessult(),
                    HasErrors = Session.Output.HasErrors,
                    CloseRunner = Session.CloseRunner,
                    DeferredMessages = Session.DeferredMessages
                };
                
                job.StatusResult = output;
                var message = new CompleteMessage {RunnerOutput = output};
                job.MessageQueue.PutMessage(message);
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

                if (job != null)
                {
                    var output =  new RunnerOutput
                    {
                        Exception = exc,
                        Output = Session.Output.GetHtmlUpdate(),
                        DialogResult = Session.Output.GetDialogRessult(),
                        HasErrors = true,
                        CloseRunner = Session.CloseRunner,
                        DeferredMessages = Session.DeferredMessages
                    };

                    job.StatusResult = output;
                    var message = new CompleteMessage { RunnerOutput = output };
                    job.MessageQueue.PutMessage(message);
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