using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Security.Accounts;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Governance
{
    [Cmdlet(VerbsCommon.Get, "User", DefaultParameterSetName = "User from name")]
    [OutputType(new[] { typeof(User) })]
    public class GetUserCommand : BaseCommand
    {       

        [Parameter(ParameterSetName = "User from name", ValueFromPipeline = true, Mandatory = true)]
        public string Name { get; set; }

        [Parameter(ParameterSetName = "User from name")]
        public string Domain { get; set; }

        [Parameter(ParameterSetName = "Current user", Mandatory = true)]
        public SwitchParameter Current { get; set; }

        [Parameter(ParameterSetName = "User from name")]
        public SwitchParameter Authenticated { get; set; }

        [Parameter(ParameterSetName = "User from name")]
        public SwitchParameter FailSilently { get; set; }

        protected override void ProcessRecord()
        {
            if (Current)
            {
                WriteObject(Context.User);
            }
            else
            {
                string name = Name;
                if (!Name.Contains(@"\"))
                {
                    if (string.IsNullOrEmpty(Domain))
                    {
                        Domain = "sitecore";
                    }
                    name = Domain + @"\" + Name;
                }
                if (name.Contains('?') || name.Contains('*'))
                    WildcardWrite(name, UserManager.GetUsers(), user => user.GetDomainName() + @"\" + user.Name);

                if (User.Exists(name))
                    WriteObject(User.FromName(name, Authenticated));
                else if (!FailSilently)
                {
                    throw new ObjectNotFoundException("User '" + name + "' could not be found");
                }
            }
        }
    }
}