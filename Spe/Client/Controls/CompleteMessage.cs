using Sitecore.Jobs.AsyncUI;
using Spe.Client.Applications;

namespace Spe.Client.Controls
{
    public class CompleteMessage : FlushMessage
    {
        public RunnerOutput RunnerOutput { get; set; }
    }
}