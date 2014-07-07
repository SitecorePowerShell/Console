using System;
using System.Data;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Security
{
    [Cmdlet(VerbsCommon.New, "Domain", DefaultParameterSetName = "Name")]
    public class NewDomainCommand : BaseCommand
    {
        [Parameter(ParameterSetName = "Name", Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter LocallyManaged { get; set; }

        [Parameter]
        public SwitchParameter PassThru { get; set; }

        protected override void ProcessRecord()
        {
            if (DomainManager.DomainExists(Name))
            {
                var error = String.Format("Cannot create a duplicate domain with name '{0}'.", Name);
                WriteError(new ErrorRecord(new DuplicateNameException(error), error, ErrorCategory.InvalidArgument, Name));
                return;
            }

            DomainManager.AddDomain(Name, LocallyManaged);

            if (PassThru)
            {
                DomainManager.GetDomain(Name);
            }
        }
    }
}