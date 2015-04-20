using System.Management.Automation;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;

namespace Cognifide.PowerShell.Commandlets.Session
{
    [Cmdlet(VerbsCommon.New, "ScriptSession", DefaultParameterSetName = "All")]
    public class NewScriptSessionCommand : BaseCommand
    {
        protected override void ProcessRecord()
        {
            WriteObject(ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false));
        }
    }
}