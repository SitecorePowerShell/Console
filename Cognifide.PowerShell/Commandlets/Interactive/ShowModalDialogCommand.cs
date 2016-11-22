using System.Collections;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "ModalDialog")]
    [OutputType(typeof (string))]
    public class ShowModalDialogCommand : BaseFormCommand
    {
        [Parameter(Mandatory = true, ParameterSetName = "Dialog from control name")]
        public string Control { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = "Dialog from Url")]
        public string Url { get; set; }

        [Parameter(ParameterSetName = "Dialog from control name")]
        public Hashtable Parameters { get; set; }

        [Parameter]
        public Hashtable HandleParameters { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction())
                {
                    WriteObject("error");
                    return;
                }

                string response = null;
                if (!string.IsNullOrEmpty(Url))
                {
                    response = JobContext.ShowModalDialog(Url, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Control))
                {
                    UrlString url = new UrlString(UIUtil.GetUri("control:" + Control));
                    url["te"] = Title ?? "Sitecore";

                    if (Parameters != null)
                    {
                        foreach (string key in Parameters.Keys)
                        {
                            url.Add(key, WebUtil.SafeEncode(Parameters[key].ToString()));
                        }
                    }

                    var message = new ShowModalDialogPsMessage(url.ToString(), WidthString, HeightString, HandleParameters);
                    PutMessage(message);
                    response = (string) message.GetResult();

                }
                WriteObject(response);
            });
        }
    }
}