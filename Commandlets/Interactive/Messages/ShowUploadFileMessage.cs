using System;
using System.Web;
using Cognifide.PowerShell.Client.Applications;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowUploadFileMessage : IMessage, IMessageWithResult
    {
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string CancelButtonName { get; private set; }
        public string OkButtonName { get; private set; }
        public string Path { get; private set; }

        private Handle jobHandle;
        public MessageQueue MessageQueue { get; private set; }
        public object Result { get; private set; }

        public ShowUploadFileMessage(string width, string height, string title,
            string description, string okButtonName, string cancelButtonName, string path)
        {
            MessageQueue = new MessageQueue();
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
            Path = path;
        }


        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected virtual void ShowUI()
        {
            string resultSig = Guid.NewGuid().ToString();

            UrlString urlString = new UrlString("/sitecore modules/Shell/PowerShell/UploadFile/PowerShellUploadFile.aspx");
            UrlHandle handle = new UrlHandle();
            handle["te"] = Title ?? string.Empty;
            handle["ds"] = Description ?? string.Empty;
            handle["ic"] = "powershell/32x32/powershell8.png";
            handle["ok"] = "OK";
            var ask = true;
            handle["path"] = Path;
            handle["mask"] = "*.*";
            handle["de"] = "txt";
            handle.Add(urlString);
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height, "", true);
        }

        /// <summary>
        ///     Starts the pipeline.
        /// </summary>
        public void Execute()
        {
            Context.ClientPage.Start(this, "Pipeline");
        }

        /// <summary>
        ///     Entry point for a pipeline.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Pipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                if (jobHandle != null)
                {
                    Context.ClientPage.ServerProperties["#pipelineJob"] = jobHandle.ToString();
                }
                ShowUI();
                args.WaitForPostBack();
            }
            else
            {
                string result = args.HasResult ? args.Result : null;
                ;
                string strJobId = StringUtil.GetString(Context.ClientPage.ServerProperties["#pipelineJob"]);
                if (!String.IsNullOrEmpty(strJobId))
                {
                    jobHandle = Handle.Parse(strJobId);
                    Job job = JobManager.GetJob(jobHandle);
                    if (job != null)
                    {
                        job.MessageQueue.PutResult(result);
                    }
                }
                else
                {
                    MessageQueue.PutResult(result);
                }
            }
        }
    }
}