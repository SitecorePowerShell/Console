using System.Linq;
using System.Management.Automation;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.Commandlets.Security.Session
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
                    foreach (var result in from instanceId in InstanceId
                        from result in WildcardFilter(instanceId, DomainAccessGuard.Sessions, s => s.SessionID)
                        where ShouldProcess(result.SessionID, "Kill session for user '" + result.UserName + "'")
                        select result)
                    {
                        DomainAccessGuard.Kick(result.SessionID);
                    }
                    break;
                case "Instance":
                    foreach (
                        var result in
                            WildcardFilter(Instance.SessionID, DomainAccessGuard.Sessions, s => s.SessionID)
                                .Where(
                                    result =>
                                        ShouldProcess(result.SessionID,
                                            "Kill session for user '" + result.UserName + "'")))
                    {
                        DomainAccessGuard.Kick(result.SessionID);
                    }
                    break;
            }
        }
    }
}