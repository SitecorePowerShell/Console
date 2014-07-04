using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Remove, "Session", DefaultParameterSetName = "InstanceId")]
    [OutputType(new[] {typeof (DomainAccessGuard.Session)})]
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
                            DomainAccessGuard.Kick(result.SessionID);
                        }
                    }
                    break;
                case "Instance":
                    foreach (
                        var result in
                            WildcardFilter(Instance.SessionID, DomainAccessGuard.Sessions, s => s.SessionID))
                    {
                        DomainAccessGuard.Kick(result.SessionID);
                    }
                    break;
            }
        }
    }
}