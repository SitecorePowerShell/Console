using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Install;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet("Receive", "File")]
    [OutputType(new[] {typeof (String)}, ParameterSetName = new[] {"Receive Media Item", "Receive File"})]
    public class ReceiveFileCommand : BaseFormCommand
    {
        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public string Description { get; set; }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Receive Media Item")]
        public Item ParentItem { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            Mandatory = true, Position = 0, ParameterSetName = "Receive File")]
        public string Folder { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                AssertDefaultSize(700, 140);

                string response = null;

                AssertDefaultSize(500, 300);
                var message = new ShowUploadFileMessage(WidthString, HeightString, Title, Description,
                    "OK", "Cancel", ParentItem != null ? (ParentItem.ID.ToString()) : Folder);

                PutMessage(message);
                var results = (string)GetResult(message);
                WriteObject(results != null ? results : "cancel");

            });
        }
    }
}