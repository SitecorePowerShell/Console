using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore.Analytics.Pipelines.StartTracking;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Install;
using Sitecore.Text;
using Sitecore.Web;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsData.Out, "Download")]
    [OutputType(typeof (string))]
    public class OutDownloadCommand : BaseShellCommand
    {

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public object InputObject { get; set; }

        [Parameter]
        public string ContentType { get; set; }

        [Parameter]
        public string Name { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                PutMessage(new OutDownloadMessage(InputObject, Name, ContentType));
            });
        }
    }
}