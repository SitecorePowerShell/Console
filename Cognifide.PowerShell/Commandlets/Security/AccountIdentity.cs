using System;
using System.Text.RegularExpressions;
using Sitecore;
using Sitecore.Diagnostics;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.Commandlets.Security
{
    public class AccountIdentity
    {
        public AccountIdentity(string name)
        {
            Assert.ArgumentNotNullOrEmpty(name, "name");

            var domain = "sitecore";
            var account = name;
            if (name.Contains(@"\"))
            {
                domain = StringUtil.GetPrefix(name, '\\');
                account = StringUtil.GetPostfix(name, '\\');
            }

            if (!Regex.IsMatch(account, @"^\w[\w\s.\\_-]*$", RegexOptions.Compiled))
            {
                throw new ArgumentException(String.Format("The name '{0}' is improperly formatted.\n\nThe name can only contain the following characters: a-z, 0-9, periods, dashes, underscores, backslashes, and spaces.", name), "name");
            }

            Domain = domain;
            Account = account;
            Name = domain + @"\" + account;
        }

        public AccountIdentity(Account account)
        {
            Assert.ArgumentNotNull(account, "account");

            Domain = account.Domain.Name;
            Account = account.LocalName;
            Name = account.Name;
        }

        public string Name { get; private set; }
        public string Domain { get; private set; }
        public string Account { get; private set; }

        public override string ToString()
        {
            return String.Format("{0}", Name);
        }

        public static implicit operator Account(AccountIdentity account)
        {
            if (Role.Exists(account.Name))
            {
                return Role.FromName(account.Name);
            }
            return User.Exists(account.Name) ? User.FromName(account.Name,false) : null;
        }

        public static implicit operator User(AccountIdentity account)
        {
            return User.Exists(account.Name) ? User.FromName(account.Name, true) : null;
        }
    }
}