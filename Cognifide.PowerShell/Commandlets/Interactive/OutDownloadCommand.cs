using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Extensions;
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

                object content;
                InputObject = InputObject.BaseObject();
                if (InputObject is Stream)
                {
                    using (var stream = InputObject as Stream)
                    {
                        byte[] bytes = new byte[stream.Length];
                        stream.Seek(0, SeekOrigin.Begin);
                        stream.Read(bytes, 0, (int)stream.Length);
                        content = bytes;
                    }
                }
                else if (InputObject is string)
                {
                    content = InputObject;
                }
                else if (InputObject is string[])
                {
                    content = (InputObject as string[]).ToList().Aggregate((accumulated, next) =>
                        accumulated + "\n" + next);
                }
                else if (InputObject is byte[])
                {
                    content = InputObject;
                }
                else
                {
                    WriteError(typeof (FormatException), "InputObject must be of type string, strings[], Stream byte[]",
                        ErrorIds.InvalidItemType, ErrorCategory.InvalidType, InputObject, true);
                    return;
                }

                PutMessage(new OutDownloadMessage(content, Name, ContentType));
            });
        }
    }
}