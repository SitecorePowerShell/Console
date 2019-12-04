using System.Collections;
using Sitecore.Jobs.AsyncUI;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.VersionSpecific.Services
{
    public class SpeJobMessageManager : IJobMessageManager
    {
        public string Confirm(string message)
        {
            return JobContext.Confirm(message);
        }

        public string ShowModalDialog(string url, string width, string height)
        {
            return JobContext.ShowModalDialog(url, width, height);
        }

        public string ShowModalDialog(string title, string controlName, string width, string height)
        {
            return JobContext.ShowModalDialog(title, controlName, width, height);
        }

        public string ShowModalDialog(Hashtable parameters, string controlName, string width, string height)
        {
            return JobContext.ShowModalDialog(parameters, controlName, width, height);
        }
    }
}
