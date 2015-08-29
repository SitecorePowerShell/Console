using System;
using System.Data;
using System.Management.Automation;
using Sitecore.Security.Domains;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.New, "Domain", DefaultParameterSetName = "Name", SupportsShouldProcess = true)]
    [OutputType(typeof (Domain))]
    public class NewDomainCommand : BaseCommand
    {
        [Parameter(ParameterSetName = "Name", Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        public string Name { get; set; }

        [Parameter]
        public SwitchParameter LocallyManaged { get; set; }

        protected override void ProcessRecord()
        {
            if (DomainManager.DomainExists(Name))
            {
                WriteError(typeof(DuplicateNameException), $"Cannot create a duplicate domain with name '{Name}'.", 
                    ErrorIds.DomainAlreadyExists, ErrorCategory.InvalidArgument, Name);
                return;
            }

            if (!ShouldProcess(Name, "Create domain")) return;

            DomainManager.AddDomain(Name, LocallyManaged);
            WriteObject(DomainManager.GetDomain(Name));
        }
    }
}