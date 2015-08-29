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
    [Cmdlet(VerbsCommon.New, "SecuritySource",DefaultParameterSetName = "Filter")]
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
        [Parameter(ParameterSetName = "Id", ValueFromPipeline = true, Position = 2)]
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
                    if (Role.Exists(Identity.Name) && (AccountType == AccountType.Role || AccountType == AccountType.Unknown))
                    {
                        source.AddAccount(new AccountString(Identity.Name, AccountType.Role));
                    }
                    else if (User.Exists(Identity.Name) && (AccountType == AccountType.User || AccountType == AccountType.Unknown))
                    {
                        source.AddAccount(new AccountString(Identity.Name, AccountType.User));
                    }
                    else
                    {
                        WriteError(typeof(ObjectNotFoundException), $"Cannot find any account of type {AccountType} with identity '{Identity.Name}'.", 
                            ErrorIds.AccountNotFound, ErrorCategory.ObjectNotFound, Identity);
                        
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