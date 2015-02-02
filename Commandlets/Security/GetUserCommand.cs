using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security
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
                    if (!this.CanFindAccount(Identity, AccountType.User)) { return; }

                    WriteObject(User.FromName(Identity.Name, Authenticated));
                    break;
            }
        }
    }
}