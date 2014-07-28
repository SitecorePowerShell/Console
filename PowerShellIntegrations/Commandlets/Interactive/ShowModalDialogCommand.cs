using System.Collections;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Query;
using Sitecore.Forms.Core.Crm;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Web;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ModalDialog")]
    [OutputType(new[] {typeof (string)})]
    public class ShowModalDialogCommand : BaseFormCommand
    {
        [Parameter(Mandatory = true, ParameterSetName = "Dialog from control name")]
        public string Control { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Dialog from Url")]
        public string Url { get; set; }

        [Parameter(ParameterSetName = "Dialog from control name")]
        public Hashtable Parameters { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                string response = null;
                if (Parameters != null)
                {
                    var hashParams = new Hashtable(Parameters.Count);
                    foreach (string key in Parameters.Keys)
                    {
                        hashParams.Add(key,WebUtil.SafeEncode(Parameters[key].ToString()));
                    }
                    response = JobContext.ShowModalDialog(hashParams, Control, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Url))
                {
                    response = JobContext.ShowModalDialog(Url, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Control))
                {
                    response = JobContext.ShowModalDialog(Title ?? "Sitecore", Control, WidthString, HeightString);
                }
                WriteObject(response);
            });
        }
    }
}