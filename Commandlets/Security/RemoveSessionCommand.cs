using System.Management.Automation;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Remove, "Session", DefaultParameterSetName = "InstanceId", SupportsShouldProcess = true)]
    public class RemoveSessionCommand : BaseCommand
    {
        [Parameter(ParameterSetName = "InstanceId", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string[] InstanceId { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Instance", ValueFromPipeline = true, Position = 0)]
        [ValidateNotNull]
        public DomainAccessGuard.Session Instance { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "InstanceId":
                    foreach (var instanceId in InstanceId)
                    {
                        foreach (
                            var result in WildcardFilter(instanceId, DomainAccessGuard.Sessions, s => s.SessionID))
                        {
                            if (ShouldProcess(result.SessionID, "Kill session for user '" + result.UserName + "'"))
                            {
                                DomainAccessGuard.Kick(result.SessionID);
                            }
                        }
                    }
                    break;
                case "Instance":
                    foreach (
                        var result in
                            WildcardFilter(Instance.SessionID, DomainAccessGuard.Sessions, s => s.SessionID))
                    {
                        if (ShouldProcess(result.SessionID, "Kill session for user '" + result.UserName + "'"))
                        {
                            DomainAccessGuard.Kick(result.SessionID);
                        }
                    }
                    break;
            }
        }
    }
}