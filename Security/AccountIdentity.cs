using System;
using Sitecore;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Security
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
                domain = StringUtil.GetPrefix(name, '\'');
                account = StringUtil.GetPostfix(name, '\'');
            }

            Domain = domain;
            Account = account;
            Name = domain + @"\" + account;
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