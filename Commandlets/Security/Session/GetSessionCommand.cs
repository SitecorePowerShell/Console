using System;
using System.Management.Automation;
using Sitecore.Web.Authentication;

namespace Cognifide.PowerShell.Commandlets.Security.Session
{
    [Cmdlet(VerbsCommon.Get, "Session", DefaultParameterSetName = "Id")]
    [OutputType(typeof (DomainAccessGuard.Session))]
    public class GetSessionCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id")]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "InstanceId", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string[] InstanceId { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Id":
                    var filter = Identity == null ? String.Empty : Identity.Name;
                    WildcardWrite(filter, DomainAccessGuard.Sessions, s => s.UserName);
                    break;
                case "InstanceId":
                    foreach (var instanceId in InstanceId)
                    {
                        WildcardWrite(instanceId, DomainAccessGuard.Sessions, s => s.SessionID);
                    }
                    break;
            }
        }
    }
}