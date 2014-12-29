using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Extensions;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Remove, "Domain", DefaultParameterSetName = "Name")]
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
                    var users = UserManager.GetUsers().Where(u => StringExtensions.Is(u.Domain.Name, Name));
                    if (users.Any(u => u.LocalName.Is("Anonymous")))
                    {
                        var error = String.Format("Cannot remove a domain with name '{0}' because it contains users.", Name);
                        WriteError(new ErrorRecord(new InvalidOperationException(error), error, ErrorCategory.InvalidOperation, Name));
                        return;
                    }
                }

                DomainManager.RemoveDomain(Name);
            }
            else
            {
                var error = String.Format("Cannot find a domain with name '{0}'.", Name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Name));
            }
        }
    }
}