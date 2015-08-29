using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.Remove, "Domain", DefaultParameterSetName = "Name", SupportsShouldProcess = true)]
    public class RemoveDomainCommand : BaseCommand
    {
        [Parameter(ParameterSetName = "Name", Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecord()
        {
            if (DomainManager.DomainExists(Name))
            {
                if (!Force)
                {
                    var users = UserManager.GetUsers().Where(u => u.Domain.Name.Is(Name));
                    if (users.Any(u => u.LocalName.Is("Anonymous")))
                    {
                        WriteError(typeof(InvalidOperationException), $"Cannot remove a domain '{Name}' because it contains users.",
                            ErrorIds.InvalidOperation, ErrorCategory.InvalidOperation, Name);
                        return;
                    }
                }

                if (ShouldProcess(Name, "Remove domain"))
                {
                    DomainManager.RemoveDomain(Name);
                }
            }
            else
            {
                WriteError(typeof(ObjectNotFoundException), $"Cannot find a domain '{Name}'.", 
                    ErrorIds.DomainNotFound, ErrorCategory.ObjectNotFound, Name);
            }
        }
    }
}