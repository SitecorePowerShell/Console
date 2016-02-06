using System.Linq;
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
        [Parameter(ParameterSetName = "Filter")]
        public SwitchParameter Authenticated { get; set; }

        [Parameter(ParameterSetName = "Filter")]
        [ValidateRange(1, int.MaxValue)]
        public int ResultPageSize { get; set; } = 7000;

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Current":
                    WriteObject(Context.User);
                    break;
                case "Filter":
                    var filter = Filter;

                    if (filter.Is("*") || filter.Is("%"))
                    {
                        int total;
                        var users =
                            new PagingIterator<MembershipUser>(
                                pageIndex => Membership.GetAllUsers(pageIndex, ResultPageSize, out total));
                        WriteUsers(users);
                    }
                    else if (filter.Contains("?") || filter.Contains("*"))
                    {
                        var pattern = filter.Replace("*", "%").Replace("?", "%");
                        int total;

                        if (filter.Contains("@"))
                        {
                            var users =
                                new PagingIterator<MembershipUser>(
                                    pageIndex =>
                                        Membership.FindUsersByEmail(pattern, pageIndex, ResultPageSize, out total));
                            WriteUsers(users);
                        }
                        else
                        {
                            var users =
                                new PagingIterator<MembershipUser>(
                                    pageIndex =>
                                        Membership.FindUsersByName(pattern, pageIndex, ResultPageSize, out total));
                            WriteUsers(users);
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

        private void WriteUsers(PagingIterator<MembershipUser> users)
        {
            if (IsParameterSpecified("ResultPageSize"))
            {
                foreach (var user in users)
                {
                    WriteObject(User.FromName(user.UserName, Authenticated));
                }
            }
            else
            {
                var accumulatedUsers = new Enumerable<User>(() => users.ToList(), o => User.FromName(((MembershipUser)o).UserName, Authenticated));
                WriteObject(accumulatedUsers, true);

            }
        }
    }
}