using System;
using System.IO;
using System.Management.Automation;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.IO;
using Sitecore.Jobs.AsyncUI;
using Spe.Commands.Interactive.Messages;
using Spe.Core.Extensions;

namespace Spe.Commands.Interactive
{
    [Cmdlet(VerbsCommunications.Send, "File")]
    [OutputType(typeof (String), ParameterSetName = new[] {"Download Item", "Download File"})]
    public class SendFileCommand : BaseFormCommand
    {
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download File")]
        [Alias("FullName", "FileName")]
        public virtual string Path { get; set; }

        [Parameter]
        public string Message { get; set; }

        [Parameter(ValueFromPipeline = true,
            Mandatory = true, Position = 0, ParameterSetName = "Download Item")]
        public Item Item { get; set; }

        [Parameter]
        public SwitchParameter NoDialog { get; set; }

        [Parameter]
        public SwitchParameter ShowFullPath { get; set; }


        protected override void ProcessRecord()
        {
            LogErrors(() =>
            {
                if (!CheckSessionCanDoInteractiveAction()) return;

                AssertDefaultSize(700, 140);
                DownloadMessage message = null;
                if (Item != null)
                {
                    if (!Item.Paths.IsMediaItem)
                    {
                        WriteVerbose($"Skipping item {Item.ID} because it is not a media item.");
                        return;
                    }
                    if (Item.InheritsFrom(TemplateIDs.MediaFolder))
                    {
                        WriteVerbose($"Skipping item {Item.ID} because it inherits from Media Folder.");
                        return;
                    }

                   message = new DownloadMessage(Item);
                }               
                else if (!string.IsNullOrEmpty(Path))
                {
                    var file = FileUtil.MapPath(Path);

                    if (!File.Exists(file))
                    {
                        PutMessage(
                            new AlertMessage($"You cannot download:\n{Path}\n\n The file could not be found."));
                        return;
                    }

                    message = new DownloadMessage(file);
                }

                if (message != null)
                {
                    message.NoDialog = NoDialog;
                    message.Message = Message;
                    message.Title = Title;
                    message.ShowFullPath = ShowFullPath;
                    message.Width = Width == 0 ? "600" : WidthString;
                    message.Height = Height == 0 ? "200" : HeightString;

                    PutMessage(message);
                    var result = message.GetResult().ToString();
                    WriteObject(result);
                }
            });
        }
    }
}