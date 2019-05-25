using System;
using System.Management.Automation;
using Sitecore.Security.Accounts;
using Spe.Core.Extensions;
using Spe.Core.Validation;

namespace Spe.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsDiagnostic.Test, "Account", DefaultParameterSetName = "Id")]
    [OutputType(typeof (Account))]
    public class TestAccountCommand : BaseSecurityCommand
    {
        [Alias("Name")]
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(RoleAndUserNames))]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Id")]
        [ValidateSet("Role", "User", "All")]
        [AutocompleteSet(nameof(AccountTypeNames))]
        public string AccountType { get; set; }

        public TestAccountCommand()
        {
            if (String.IsNullOrEmpty(AccountType))
            {
                AccountType = "All";
            }
        }

        protected override void ProcessRecord()
        {
            var checkRole = AccountType.Is("Role");
            var checkUser = AccountType.Is("User");
            var checkAll = AccountType.Is("All");


            var roleExists = ((checkAll || checkRole) && Role.Exists(Identity.Name));

            var userExists = ((checkAll || checkUser) && User.Exists(Identity.Name));
            
            var exists = false;
            if (checkAll && (roleExists || userExists))
            {
                exists = true;
            }
            else
            {
                exists = (checkRole) ? roleExists : checkUser && userExists;
            }

            WriteObject(exists);
        }
    }
}