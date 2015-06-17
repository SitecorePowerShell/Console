using System;
using System.Collections;
using System.Web.ModelBinding;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.Install;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowModalDialogPsMessage : IMessage, IMessageWithResult
    {
        private Handle jobHandle;
        [NonSerialized] private readonly MessageQueue messageQueue;

        public ShowModalDialogPsMessage(string url, string width, string height, Hashtable handleParams)
        {
            messageQueue = new MessageQueue();
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            HandleParams = handleParams;
            Url = url;
        }

        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Url { get; private set; }
        public Hashtable HandleParams { get; set; }

        /// <summary>
        ///     Starts the pipeline.
        /// </summary>
        public void Execute()
        {
            Context.ClientPage.Start(this, "Pipeline");
        }

        public MessageQueue MessageQueue
        {
            get { return messageQueue; }
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected virtual void ShowUI()
        {
            var urlString = new UrlString(Url);
            if (HandleParams != null && HandleParams.Count > 0)
            {
                var handle = new UrlHandle();
                foreach (string key in HandleParams.Keys)
                {
                    var value = HandleParams[key];
                    if ((value is string) &&
                        ((string) value).StartsWith("packPath:", StringComparison.OrdinalIgnoreCase))
                    {
                        string strValue = (string) value;
                        strValue = strValue.Substring(9);
                        handle[key] = ApplicationContext.StoreObject(strValue);
                    }
                    else
                    {
                        handle[key] = value != null ? value.ToString() : string.Empty;
                    }
                }
                handle.Add(urlString);
            }

            Context.ClientPage.ClientResponse.ShowModalDialog(urlString.ToString(), Width, Height, string.Empty, true);

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
                var result = args.HasResult ? args.Result : null;
                if (string.IsNullOrEmpty(result))
                {
                    result = "undetermined";
                }
                var strJobId = StringUtil.GetString(Context.ClientPage.ServerProperties["#pipelineJob"]);
                if (!String.IsNullOrEmpty(strJobId))
                {
                    jobHandle = Handle.Parse(strJobId);
                    var job = JobManager.GetJob(jobHandle);
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