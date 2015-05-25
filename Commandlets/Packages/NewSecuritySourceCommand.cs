using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Security;
using Sitecore;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Install.Security;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Packages
{
    [Cmdlet(VerbsCommon.New, "SecuritySource")]
    [OutputType(typeof (SecuritySource))]
    public class NewSecuritySourceCommand : BasePackageCommand
    {
        private SecuritySource source;

        [Parameter(ParameterSetName = "Account", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        public Account Account { get; set; }

        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public AccountIdentity Identity { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipeline = true, Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string[] Filter { get; set; }

        [Parameter(ParameterSetName = "Filter", ValueFromPipeline = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public AccountType AccountType { get; set; }

        [Parameter(Position = 0, Mandatory = true)]
        public string Name { get; set; }

        protected override void BeginProcessing()
        {
            source = new SecuritySource { Name = Name }; //Create source – source should be based on BaseSource              
        }

        protected override void ProcessRecord()
        {
            switch (ParameterSetName)
            {
                case "Account":
                    source.AddAccount(new AccountString(Account.Name, Account.AccountType));
                    break;
                case "Id":
                    if (Role.Exists(Identity.Name))
                    {
                        source.AddAccount(new AccountString(Account.Name, AccountType.Role));
                    }
                    else if (User.Exists(Identity.Name))
                    {
                        source.AddAccount(new AccountString(Account.Name, AccountType.User));
                    }
                    else
                    {
                        var error = String.Format("Cannot find any user or role with identity '{0}'.", Identity.Name);
                        WriteError(new ErrorRecord(new ObjectNotFoundException(error), error,
                            ErrorCategory.ObjectNotFound, Identity));
                        
                    }
                    break;
                case "Filter":
                    foreach (var filter in Filter)
                    {
                        if (AccountType == AccountType.User || AccountType == AccountType.Unknown)
                        {
                            WildcardFilter(filter, UserManager.GetUsers(), user => user.Name)
                                .ForEach(user => source.AddAccount(new AccountString(user.Name, AccountType.User)));
                        }
                        if (AccountType == AccountType.Role || AccountType == AccountType.Unknown)
                        {
                            WildcardFilter(filter, Context.User.Delegation.GetManagedRoles(true),
                                role => role.Name)
                                .ForEach(role => source.AddAccount(new AccountString(role.Name, AccountType.Role)));
                        }
                    }
                    break;
            }
        }

        protected override void EndProcessing()
        {
            WriteObject(source, false);
        }
    }
}