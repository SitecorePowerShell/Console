using System;
using System.Management.Automation;
using Sitecore;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets
{
    [Cmdlet("Get", "User", DefaultParameterSetName = "From Name")]
    [OutputType(new[] {typeof (User)})]
    public class GetUserCommand : BaseCommand
    {
        [Parameter(ValueFromPipeline = true, Position = 0, ParameterSetName = "From Name")]
        public string Name { get; set; }

        [Parameter(ParameterSetName = "Current User")]
        public SwitchParameter Current { get; set; }

        protected override void ProcessRecord()
        {
            if (Current.IsPresent)
            {
                WriteObject(Context.User, false);
            }

            if (!String.IsNullOrEmpty(Name))
            {
                WildcardWrite(Name, UserManager.GetUsers(), user => user.Name);
            }
            else
            {
                WriteObject(UserManager.GetUsers(), true);
            }
        }
    }
}