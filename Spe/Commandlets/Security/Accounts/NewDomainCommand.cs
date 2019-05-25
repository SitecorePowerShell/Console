using System.Data;
using System.Management.Automation;
using Sitecore.Security.Domains;
using Sitecore.SecurityModel;
using Spe.Core.Validation;

namespace Spe.Commandlets.Security.Accounts
{
    [Cmdlet(VerbsCommon.New, "Domain", DefaultParameterSetName = "Name", SupportsShouldProcess = true)]
    [OutputType(typeof (Domain))]
    public class NewDomainCommand : BaseSecurityCommand
    {
        [Parameter(ParameterSetName = "Name", Mandatory = true, Position = 0)]
        [ValidateNotNullOrEmpty]
        [AutocompleteSet(nameof(DomainNames))]
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