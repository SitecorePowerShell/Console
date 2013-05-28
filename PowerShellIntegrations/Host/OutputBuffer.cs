using System;
using System.Collections.Generic;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public class OutputBuffer : List<OutputLine>
    {
        [Flags]
        public enum ExecutionPhases
        {
            None,
            BeforeExecution,
            AfterExecution
        }

        private const string FlushBeforeExecution = "{7316EE03-242B-40FA-8FAD-EAC85168117C}";
        private const string FlushAfterExecution = "{D8662901-DE9B-47E4-A327-64F05F94BB1E}";

        public void FlushBuffer(string flushOutputBuffer, ExecutionPhases executionPhases)
        {
            if (!string.IsNullOrEmpty(flushOutputBuffer) &&
                ((executionPhases == ExecutionPhases.BeforeExecution && flushOutputBuffer.Contains(FlushBeforeExecution)) ||
                 (executionPhases == ExecutionPhases.AfterExecution && flushOutputBuffer.Contains(FlushAfterExecution))))
            {
                Clear();
            }
        }
    }
}