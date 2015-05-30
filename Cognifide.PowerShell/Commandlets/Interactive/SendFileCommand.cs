using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.Commandlets.Interactive.Messages;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Cmdlet(VerbsCommunications.Send, "File")]
    [OutputType(typeof (String), ParameterSetName = new[] {"Download Item", "Download File"})]
    public class SendFileCommand : BaseFormCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download File")]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter(ParameterSetName = "Download Item")]
        [Parameter(ParameterSetName = "Download File")]
        public string Message { get; set; }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download Item")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Download Item")]
        [Parameter(ParameterSetName = "Download File")]
        public SwitchParameter NoDialog { get; set; }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                AssertDefaultSize(700, 140);

                string response;
                var hashParams = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
                if (Item != null)
                {
                    if (NoDialog.IsPresent)
                    {
                        PutMessage(new DownloadMessage(Item));
                        return;
                    }
                    hashParams.Add("id", Item.ID);
                    hashParams.Add("db", Item.Database.Name);
                }
                else if (!string.IsNullOrEmpty(Path))
                {
                    var file = FileUtil.MapPath(Path);
                    if (!File.Exists(file))
                    {
                        PutMessage(
                            new AlertMessage("You cannot download:\n" + Path + "\n\n The file could not be found."));
                        return;
                    }

                    if (NoDialog.IsPresent)
                    {
                        PutMessage(new DownloadMessage(Path));
                        return;
                    }

                    var str1 = FileUtil.MapPath("/");
                    var str2 = FileUtil.MapPath(Settings.DataFolder);
                    if (!file.StartsWith(str1, StringComparison.InvariantCultureIgnoreCase) &&
                        !file.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase))
                    {
                        PutMessage(new AlertMessage(
                            "Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n" +
                            "Copy the file to the Sitecore Data folder and try again."));
                        return;
                    }
                    hashParams.Add("fn", Path);
                }
                hashParams.Add("te", Message ?? string.Empty);
                hashParams.Add("cp", Title ?? string.Empty);
                response = JobContext.ShowModalDialog(
                    hashParams,
                    "DownloadFile",
                    Width == 0 ? "600" : WidthString,
                    Height == 0 ? "200" : HeightString);
                WriteObject(response);
            });
        }
    }
}