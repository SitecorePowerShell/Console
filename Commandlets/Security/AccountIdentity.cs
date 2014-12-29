using System;
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
    }
}