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

                // Drain remaining output in jquery.terminal's native format
                // (jsterm). The ISE's terminal is a jquery.terminal instance
                // (#1458) and its echo path parses these format strings
                // natively, matching what the standalone SPE Console does.
                // Using GetConsoleUpdate instead of GetHtmlUpdate is also what
                // gives us correct Write-Host -NoNewline handling via its
                // "wait for terminator" line-buffering.
                var finalOutputBuffer = new System.Text.StringBuilder();
                Session.Output.GetConsoleUpdate(finalOutputBuffer, 131072);
                var output = new RunnerOutput
                {
                    Exception = null,
                    Output = finalOutputBuffer.ToString(),
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
                PowerShellLog.Error("[Runner] action=executeScript status=aborted", taex);
                if (!Environment.HasShutdownStarted)
                {
                    Thread.ResetAbort();
                }
            }
            catch (Exception exc)
            {
                PowerShellLog.Error("[Runner] action=executeScript status=failed", exc);

                if (job != null)
                {
                    // jsterm format - same as the success-path drain above.
                    var errorOutputBuffer = new System.Text.StringBuilder();
                    Session.Output.GetConsoleUpdate(errorOutputBuffer, 131072);
                    var output =  new RunnerOutput
                    {
                        Exception = exc,
                        Output = errorOutputBuffer.ToString(),
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
                // Post-execution cleanup - don't signal the client to purge its
                // terminal output, we just finished consuming it through the poll
                // loop and want to free server-side memory.
                Session.Output.ClearSilent();
                if (AutoDispose)
                {
                    Session.Dispose();
                }
            }
        }
    }
}