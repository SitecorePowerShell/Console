using System.Management.Automation;
using System.Web.Security;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore;
using Sitecore.Common;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Get, "User", DefaultParameterSetName = "Id")]
    [OutputType(typeof (User))]
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

                    if (filter.Contains("?") || filter.Contains("*"))
                    {
                        if (filter.Contains("@"))
                        {
                            var pattern = filter.Replace("*", "%").Replace("?", "%");
                            var emailUsers =
                                new Enumerable<User>(() => Membership.FindUsersByEmail(pattern).GetEnumerator(),
                                    o => User.FromName(((MembershipUser) o).UserName, false));
                            WriteObject(emailUsers, true);
                        }
                        else
                        {
                            var users = WildcardFilter(filter, UserManager.GetUsers(), user => user.Name);
                            WriteObject(users, true);
                        }
                    }
                    break;
                default:
                    if (this.CanFindAccount(Identity, AccountType.User))
                    {
                        WriteObject(User.FromName(Identity.Name, Authenticated));
                    }
                    break;
            }
        }
    }
}