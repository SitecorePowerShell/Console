using System.Collections;
using System.Management.Automation;

namespace Spe.Commands.Data
{
    [Cmdlet(VerbsCommon.New, "PSObject")]
    [OutputType(typeof(PSObject))]
    public class NewPSObjectCommand : BaseCommand
    {
        [Parameter(Mandatory = true, Position = 0, ValueFromPipeline = true)]
        public Hashtable Property { get; set; }

        protected override void ProcessRecord()
        {
            var result = new PSObject();
            foreach (DictionaryEntry entry in Property)
            {
                result.Properties.Add(new PSNoteProperty(entry.Key.ToString(), entry.Value));
            }
            WriteObject(result);
        }
    }
}
