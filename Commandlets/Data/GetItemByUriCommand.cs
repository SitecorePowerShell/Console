using System;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Get, "ItemByUri")]
    [OutputType(new[] {typeof (Item)})]
    public class GetItemByUriCommand : BaseCommand
    {
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        [Parameter()]
        public string ItemUri { get; set; }

        protected override void ProcessRecord()
        {
            WriteError(new ErrorRecord(new Exception("This commandlet have been deprecated. Use Get-Item master:\\ -Uri \"sitecore://{database}/{ID}?lang=_lang_&ver=_ver_\" instead."), "sitecore_commandlet_deprecated", ErrorCategory.NotImplemented, null));            
        }
    }
}