﻿using System.Collections;
using System.Management.Automation;
using Sitecore;
using Sitecore.Text;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commands.Interactive.Messages;
using Spe.Core.VersionDecoupling;

namespace Spe.Commands.Interactive
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
                    var jobUiManager = TypeResolver.Resolve<IJobMessageManager>();
                    response = jobUiManager.ShowModalDialog(Url, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Control))
                {
                    var url = new UrlString(UIUtil.GetUri("control:" + Control))
                    {
                        ["te"] = Title ?? "Sitecore"
                    };

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