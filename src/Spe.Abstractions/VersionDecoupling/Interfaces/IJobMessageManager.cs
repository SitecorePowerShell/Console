using System.Collections;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IJobMessageManager
    {
        string Confirm(string message);
        string ShowModalDialog(string url, string width, string height);
        string ShowModalDialog(string title, string controlName, string width, string height);
        string ShowModalDialog(Hashtable parameters, string controlName, string width, string height);
    }
}