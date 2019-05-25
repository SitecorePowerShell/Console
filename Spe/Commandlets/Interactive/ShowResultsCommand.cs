using System.Collections;
using System.Linq;
using System.Management.Automation;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Commandlets.Interactive.Messages;
using Spe.Core.Host;
using Spe.Core.VersionDecoupling;

namespace Spe.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Show, "Result")]
    [OutputType(typeof (string))]
    public class ShowResultsCommand : BaseFormCommand
    {
        [Parameter(ParameterSetName = "Custom Viewer from Control Name", Mandatory = true)]
        public string Control { get; set; }

        [Parameter(ParameterSetName = "Custom Viewer from Url", Mandatory = true)]
        public string Url { get; set; }

        [Parameter(ParameterSetName = "Custom Viewer from Control Name")]
        public string[] Parameters { get; set; }

        [Parameter(ParameterSetName = "Text Results")]
        public SwitchParameter Text { get; set; }

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

                if (Text.IsPresent)
                {
                    if (SessionState.PSVariable.Get("ScriptSession").Value is ScriptSession session)
                    {
                        var ph = Host.PrivateData.BaseObject as ScriptingHostPrivateData;
                        var message = new ShowResultsMessage(session.Output.ToHtml(), WidthString, HeightString,
                            ph?.ForegroundColor.ToString() ?? string.Empty,
                            ph?.BackgroundColor.ToString() ?? string.Empty);

                        PutMessage(message);
                        var results = (object[])message.GetResult();
                    }
                }
                else if (Parameters != null)
                {
                    var hashParams =
                        new Hashtable(Parameters.ToDictionary(p => p.ToString().Split('|')[0],
                            p => WebUtil.SafeEncode(p.ToString().Split('|')[1])));
                    var jobUiManager = TypeResolver.Resolve<IJobUiManager>();
                    response = jobUiManager.ShowModalDialog(hashParams, Control, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Url))
                {
                    var jobUiManager = TypeResolver.Resolve<IJobUiManager>();
                    response = jobUiManager.ShowModalDialog(Url, WidthString, HeightString);
                }
                else if (!string.IsNullOrEmpty(Control))
                {
                    var jobUiManager = TypeResolver.Resolve<IJobUiManager>();
                    response = jobUiManager.ShowModalDialog(Title ?? "Sitecore", Control, WidthString, HeightString);
                }
                WriteObject(response);
            });
        }
    }
}