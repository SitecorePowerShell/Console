using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Resources.Media;
using Sitecore.Shell;
using Sitecore.Shell.Framework;
using Sitecore.Text;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet("Download", "File")]
    public class DownloadFileCommand : BaseFormCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, 
            Mandatory = true, Position = 0, ParameterSetName = "FileInput")]
        public string FullName
        {
            get;
            set;
        }

        [Parameter(ParameterSetName = "ItemInput")]
        [Parameter(ParameterSetName = "FileInput")]
        public string Message
        {
            get;
            set;
        }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "ItemInput")]
        public Item Item
        {
            get;
            set;
        }

        [Parameter(ParameterSetName = "ItemInput")]
        [Parameter(ParameterSetName = "FileInput")]
        public SwitchParameter NoDialog
        {
            get;
            set;
        }


        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                string response = null;
                var hashParams = new Hashtable();
                if (Item != null)
                {
                    if (NoDialog.IsPresent)
                    {
                        JobContext.MessageQueue.PutMessage(new DownloadMessage(Item));
                        return;
                    }
                    hashParams.Add("id", Item.ID);
                    hashParams.Add("db", Item.Database.Name);
                }
                else if (!string.IsNullOrEmpty(FullName))
                {
                    var file = FileUtil.MapPath(FullName);
                    if (!File.Exists(file))
                    {
                        JobContext.Alert("You cannot download:\n" + FullName + "\n\n The file could not be found.");
                        return;
                    }
                    
                    if (NoDialog.IsPresent)
                    {
                        JobContext.MessageQueue.PutMessage(new DownloadMessage(FullName));
                        return;
                    }

                    string str1 = FileUtil.MapPath("/");
                    string str2 = FileUtil.MapPath(Sitecore.Configuration.Settings.DataFolder);
                    if (!file.StartsWith(str1, StringComparison.InvariantCultureIgnoreCase) &&
                        !file.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase))
                    {
                        JobContext.Alert(
                            "Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n" +
                            "Copy the file to the Sitecore Data folder and try again.");
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