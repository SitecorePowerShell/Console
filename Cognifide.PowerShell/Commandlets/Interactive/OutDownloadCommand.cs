using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings.Authorization;

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

        [Parameter(ValueFromPipelineByPropertyName = true)]
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
                else if (InputObject is FileInfo)
                {
                    content = InputObject;
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
                    WriteError(typeof(FormatException), "InputObject must be of type string, strings[], Stream byte[]",
                        ErrorIds.InvalidItemType, ErrorCategory.InvalidType, InputObject, true);
                    WriteObject(false);
                    return;
                }

                if (!WebServiceSettings.ServiceEnabledHandleDownload ||
                    !ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceHandleDownload,
                        Sitecore.Context.User.Name, false))
                {
                    WriteError(typeof(FormatException), "Handle Download Service is disabled or user is not authorized.",
                        ErrorIds.InsufficientSecurityRights, ErrorCategory.PermissionDenied, InputObject, true);
                    WriteObject(false);
                    return;
                }

                LogErrors(() =>
                {
                    WriteObject(true);
                    PutMessage(new OutDownloadMessage(content, Name, ContentType));
                });
            });
        }

    }
}