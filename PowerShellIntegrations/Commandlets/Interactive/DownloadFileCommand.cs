using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Download", "File")]
    [OutputType(new[] {typeof (String)}, ParameterSetName = new[] {"Download Item", "Download File"})]
    public class DownloadFileCommand : BaseFormCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download File")]
        public string FullName { get; set; }

        [Parameter(ParameterSetName = "Download Item")]
        [Parameter(ParameterSetName = "Download File")]
        public string Message { get; set; }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download Item")]
        public Item Item { get; set; }

        [Parameter(ParameterSetName = "Download Item")]
        [Parameter(ParameterSetName = "Download File")]
        public SwitchParameter NoDialog { get; set; }

        public DownloadFileCommand()
        {
            Width = 700;
            Height = 140;
        }

        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                string response = null;
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
                else if (!string.IsNullOrEmpty(FullName))
                {
                    string file = FileUtil.MapPath(FullName);
                    if (!File.Exists(file))
                    {
                        PutMessage(
                            new AlertMessage("You cannot download:\n" + FullName + "\n\n The file could not be found."));
                        return;
                    }

                    if (NoDialog.IsPresent)
                    {
                        PutMessage(new DownloadMessage(FullName));
                        return;
                    }

                    string str1 = FileUtil.MapPath("/");
                    string str2 = FileUtil.MapPath(Sitecore.Configuration.Settings.DataFolder);
                    if (!file.StartsWith(str1, StringComparison.InvariantCultureIgnoreCase) &&
                        !file.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase))
                    {
                        PutMessage(new AlertMessage(
                            "Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n" +
                            "Copy the file to the Sitecore Data folder and try again."));
                        return;
                    }
                    hashParams.Add("fn", FullName);
                }
                hashParams.Add("te", Message ?? string.Empty);
                hashParams.Add("cp", Title ?? string.Empty);
                response = JobContext.ShowModalDialog(
                    hashParams,
                    "DownloadFile",
                    Width == 0 ? "600" : WidthString,
                    Height == 0 ? "140" : HeightString);
                WriteObject(response);
            });
        }
    }
}