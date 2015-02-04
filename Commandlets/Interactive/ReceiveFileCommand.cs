using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore;
using Sitecore.Data;
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
    [Cmdlet(VerbsCommunications.Receive, "File")]
    [OutputType(typeof(String), ParameterSetName = new[] { "Receive Media Item", "Receive File", "Receive Media Item Advanced" })]
    public class ReceiveFileCommand : BaseFormCommand
    {
        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public string Description { get; set; }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Receive Media Item")]
        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Receive Media Item Advanced")]
        public Item ParentItem { get; set; }

        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            Mandatory = true, Position = 0, ParameterSetName = "Receive File")]
        public string Path { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public override string Title { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public string CancelButtonName { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public string OkButtonName { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        public SwitchParameter Versioned { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        public string Language { get; set; }

        [Parameter(ParameterSetName = "Receive File")]
        [Parameter(ParameterSetName = "Receive Media Item")]
        public SwitchParameter Overwrite { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item")]
        [Parameter(ParameterSetName = "Receive File")]
        public SwitchParameter Unpack { get; set; }

        [Parameter(ParameterSetName = "Receive Media Item Advanced", Mandatory = true)]
        public SwitchParameter AdvancedDialog { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (AdvancedDialog)
                {
                    AssertDefaultSize(500, 650);
                }
                else
                {
                    AssertDefaultSize(500, 300);                    
                }


                string response = null;

                var message = new ShowUploadFileMessage(WidthString, HeightString, Title, Description,
                    OkButtonName ?? "OK", CancelButtonName ?? "Cancel", ParentItem != null ? (ParentItem.ID.ToString()) : Path,
                    Versioned, Language, Overwrite, Unpack, AdvancedDialog);

                PutMessage(message);
                var result = (string)GetResult(message);
                
                ID itemId;
                if (ID.TryParse(result, out itemId))
                {
                    var item = ParentItem.Database.GetItem(itemId);
                    WriteItem(item);
                }
                else
                {
                    WriteObject(string.IsNullOrEmpty(result) ? "cancel" : result);
                }


            });
        }
    }
}