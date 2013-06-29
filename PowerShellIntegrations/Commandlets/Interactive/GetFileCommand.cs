using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive
{
    [Cmdlet(VerbsCommon.Get, "File", SupportsShouldProcess = true, DefaultParameterSetName = "FullName")]
    public class GetFileCommand : BaseFormCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Mandatory = true, Position = 0)]
        public string FullName { get; set; }

        [Parameter()]
        public string Message { get; set; }
        
        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                var file = FileUtil.MapPath(FullName);
                if (!File.Exists(file))
                {
                    JobContext.Alert("You cannot download:\n" + FullName + "\n\n The file could not be found.");
                    return;
                }
                string str1 = FileUtil.MapPath("/");
                string str2 = FileUtil.MapPath(Sitecore.Configuration.Settings.DataFolder);
                if (!file.StartsWith(str1, StringComparison.InvariantCultureIgnoreCase) &&
                    !file.StartsWith(str2, StringComparison.InvariantCultureIgnoreCase))
                {
                    JobContext.Alert("Files from outside of the Sitecore Data and Website folders cannot be downloaded.\n\n"+
                        "Copy the file to the Sitecore Data folder and try again.");
                    return;
                }

                string response = null;
                var hashParams = new Hashtable();
                hashParams.Add("te", Message ?? string.Empty);
                hashParams.Add("fn", FullName);
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