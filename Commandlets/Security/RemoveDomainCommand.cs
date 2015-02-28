using System;
using System.Data;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Security.Accounts;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Commandlets.Security
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
                        var error = String.Format("Cannot remove a domain '{0}' because it contains users.", Name);
                        WriteError(new ErrorRecord(new InvalidOperationException(error), error,
                            ErrorCategory.InvalidOperation, Name));
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
                var error = String.Format("Cannot find a domain '{0}'.", Name);
                WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound, Name));
            }
        }
    }
}