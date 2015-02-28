using System;
using System.Data;
using System.Management.Automation;
using Sitecore.Security.Domains;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Commandlets.Security
{
    [Cmdlet(VerbsCommon.Get, "Domain", DefaultParameterSetName = "Name")]
    [OutputType(typeof (Domain))]
    public class GetDomainCommand : BaseCommand
    {
        [Parameter(ParameterSetName = "Name")]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            if (!String.IsNullOrEmpty(Name))
            {
                if (DomainManager.DomainExists(Name))
                {
                    WriteObject(DomainManager.GetDomain(Name));
                }
                else
                {
                    var error = String.Format("Cannot find a domain with name '{0}'.", Name);
                    WriteError(new ErrorRecord(new ObjectNotFoundException(error), error, ErrorCategory.ObjectNotFound,
                        Name));
                }
            }
            else
            {
                WildcardWrite(String.Empty, DomainManager.GetDomains(), d => d.Name);
            }
        }
    }
}