using System.Text;
using System.Web;
using Sitecore;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Host;

namespace Spe.Client.Controls
{
    /// <summary>
    ///     Streams a script session's output buffer into a page's
    ///     jquery.terminal while the script runs. Shared by the ISE and the
    ///     Runner. The page implements the client side of the contract:
    ///     spe.appendOutput, spe.commitPartialOutput, spe.updatePartialOutput,
    ///     spe.finalizePartial and spe.clearOutput.
    ///
    ///     The tracking state lives in the page's ServerProperties, so it
    ///     persists per page across Sheer requests; this object is stateless.
    /// </summary>
    public class TerminalOutputStreamer
    {
        // Tracks how much of session.Output has been sent to the client by
        // the page's streaming drain. All lines at indices < CommittedLineCount
        // have been sent to the client as committed (non-partial) lines.
        // When HasPendingPartial is true, the line at CommittedLineCount
        // is the currently-open partial on the client - it may still grow
        // on the server and will be re-read on the next poll.
        private static int CommittedLineCount
        {
            get => int.TryParse(StringUtil.GetString(Context.ClientPage.ServerProperties["OutputCommittedLineCount"]), out var v) ? v : 0;
            set => Context.ClientPage.ServerProperties["OutputCommittedLineCount"] = value.ToString();
        }

        public bool HasPendingPartial
        {
            get => StringUtil.GetString(Context.ClientPage.ServerProperties["OutputHasPendingPartial"]) == "1";
            private set => Context.ClientPage.ServerProperties["OutputHasPendingPartial"] = value ? "1" : string.Empty;
        }

        /// <summary>
        ///     One poll cycle: if the script called Clear-Host, purge the
        ///     browser's terminal output before appending any new output.
        /// </summary>
        public void Poll(ScriptSession session)
        {
            if (session.Output.ConsumeClearPending())
            {
                SheerResponse.Eval("spe.clearOutput();");
                Reset();
            }

            Drain(session);
        }

        /// <summary>
        ///     Forgets what was sent; the next drain starts from line 0.
        /// </summary>
        public void Reset()
        {
            CommittedLineCount = 0;
            HasPendingPartial = false;
        }

        /// <summary>
        ///     If the drain left a pending partial on the client, finalize it -
        ///     the content the client has rendered is the final text of that
        ///     partial, and subsequent appendOutput calls must start a fresh
        ///     visual line.
        /// </summary>
        public void FinalizePartial()
        {
            if (!HasPendingPartial) return;
            SheerResponse.Eval("spe.finalizePartial();");
            HasPendingPartial = false;
        }

        /// <summary>
        ///     Streams pending output to the terminal in jquery.terminal
        ///     native format (jsterm), supporting inline-append for
        ///     Write-Host -NoNewline. Iterates session.Output by index,
        ///     groups lines into batches (up to a terminator or the end of
        ///     the buffer), and emits:
        ///       - spe.appendOutput(jsterm)          for terminated batches
        ///         (new committed line)
        ///       - spe.commitPartialOutput(jsterm)   when a previously
        ///         pending partial is now frozen by a terminator
        ///       - spe.updatePartialOutput(jsterm)   for the unterminated
        ///         tail (the line may still grow on subsequent polls)
        ///
        ///     After the drain, moves the OutputBuffer's update pointer to the
        ///     committed lines so that ScriptRunner's later GetConsoleUpdate
        ///     drain does not re-emit them.
        /// </summary>
        public void Drain(ScriptSession session)
        {
            var output = session.Output;
            var committed = CommittedLineCount;
            var hasPending = HasPendingPartial;

            var totalLines = output.Count;

            // The buffer was cleared since the last poll (e.g. by Clear-Host
            // or a session reset). Reset our tracking.
            if (totalLines < committed)
            {
                committed = 0;
                hasPending = false;
            }

            // Find the boundary between terminated and unterminated content.
            int lastTerminated = -1;
            for (int i = totalLines - 1; i >= committed; i--)
            {
                if (output[i].Terminated)
                {
                    lastTerminated = i;
                    break;
                }
            }

            // Emit all terminated content as a single appendOutput (or
            // commitPartialOutput if there was a pending partial). One
            // echo() call with the full concatenated jsterm string.
            if (lastTerminated >= committed)
            {
                var sb = new StringBuilder();
                for (int i = committed; i <= lastTerminated; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                var encoded = HttpUtility.JavaScriptStringEncode(sb.ToString(), true);
                if (hasPending)
                {
                    SheerResponse.Eval($"spe.commitPartialOutput({encoded});");
                    hasPending = false;
                }
                else
                {
                    SheerResponse.Eval($"spe.appendOutput({encoded});");
                }
                committed = lastTerminated + 1;
            }

            // If there is an unterminated tail, emit it as a partial.
            if (committed < totalLines)
            {
                var sb = new StringBuilder();
                for (int i = committed; i < totalLines; i++)
                {
                    output[i].GetLine(sb, OutputLine.FormatResponseJsterm);
                }
                var encoded = HttpUtility.JavaScriptStringEncode(sb.ToString(), true);
                SheerResponse.Eval($"spe.updatePartialOutput({encoded});");
                hasPending = true;
            }

            // Sync the OutputBuffer's update pointer with the committed lines
            // so the ScriptRunner's end-of-run drain (GetConsoleUpdate) does
            // not re-emit them. Not past them: a pending partial and lines
            // added since our snapshot must still reach the final drain.
            output.MarkSent(committed);

            CommittedLineCount = committed;
            HasPendingPartial = hasPending;
        }

        /// <summary>
        ///     JavaScript that delivers the end-of-run output (what the drains
        ///     did not send, see ScriptRunner) after the last poll. A pending
        ///     partial is replaced by the final text rather than followed by
        ///     it: the final drain starts at that partial line.
        /// </summary>
        public string FinishScript(string finalJsterm, bool clearHost)
        {
            var script = new StringBuilder();
            if (clearHost)
            {
                script.Append("spe.clearOutput();");
                HasPendingPartial = false;
            }

            if (!string.IsNullOrEmpty(finalJsterm))
            {
                var encoded = HttpUtility.JavaScriptStringEncode(finalJsterm, true);
                script.Append(HasPendingPartial
                    ? $"spe.commitPartialOutput({encoded});"
                    : $"spe.appendOutput({encoded});");
            }
            else if (HasPendingPartial)
            {
                script.Append("spe.finalizePartial();");
            }

            HasPendingPartial = false;
            return script.ToString();
        }
    }
}
