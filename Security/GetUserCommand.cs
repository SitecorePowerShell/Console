using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.Get, "User", DefaultParameterSetName = "Id")]
    [OutputType(new[] {typeof (User)})]
    public class GetUserCommand : BaseCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", Mandatory = true)]
        [ValidateNotNullOrEmpty]
        public string Filter { get; set; }

        [Parameter(ParameterSetName = "Current", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter(ParameterSetName = "Id")]
        public SwitchParameter Authenticated { get; set; }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Current":
                    WriteObject(Context.User);
                    break;
                case "Filter":
                    var filter = Filter;
                    if (!filter.Contains("?") && !filter.Contains("*")) return;

                    var users = WildcardFilter(filter, UserManager.GetUsers(), user => user.Name);
                    WriteObject(users, true);
                    break;
                default:
                    var name = Identity.Name;

                    if (User.Exists(name))
                    {
                        WriteObject(User.FromName(name, Authenticated));
                    }
                    else
                    {
                        var error = String.Format("Cannot find an account with identity '{0}'.", name);
                        WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Identity));
                    }
                    break;
            }
        }
    }
}