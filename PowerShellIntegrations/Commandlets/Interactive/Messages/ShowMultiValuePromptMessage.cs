using System;
using System.Web;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowMultiValuePromptMessage : IMessage
    {

        public object[] Parameters { get; private set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string CancelButtonName { get; private set; }
        public string OkButtonName { get; private set; }

        private Handle jobHandle;

        public ShowMultiValuePromptMessage(object[] parameters, string width, string height, string title, string description, string okButtonName, string cancelButtonName)
        {
            jobHandle = JobContext.JobHandle;
            Parameters = parameters;
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
        }


        /// <summary>
        /// Shows a confirmation dialog.
        /// 
        /// </summary>
        protected virtual void ShowUI()
        {
            string resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Session[resultSig] = Parameters;
            UrlString urlString = new UrlString(UIUtil.GetUri("control:PowerShellMultiValuePrompt"));
            urlString.Add("sid", resultSig);
            if (!string.IsNullOrEmpty(Title))
            {
                urlString.Add("te", Title);
            }
            if (!string.IsNullOrEmpty(Description))
            {
                urlString.Add("ds", Description);
            }
            if (!string.IsNullOrEmpty(OkButtonName))
            {
                urlString.Add("ob", OkButtonName);
            }
            if (!string.IsNullOrEmpty(CancelButtonName))
            {
                urlString.Add("cb", CancelButtonName);
            }
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height, "", true);
        }

        /// <summary>
        /// Starts the pipeline.
        /// 
        /// </summary>
        public void Execute()
        {
            Context.ClientPage.Start(this, "Pipeline");
        }

        /// <summary>
        /// Entry point for a pipeline.
        /// 
        /// </summary>
        /// <param name="args">The arguments.</param>
        public void Pipeline(ClientPipelineArgs args)
        {
            if (!args.IsPostBack)
            {
                Context.ClientPage.ServerProperties["#pipelineJob"] = jobHandle.ToString();
                ShowUI();
                args.WaitForPostBack();
            }
            else
            {
                jobHandle = Handle.Parse(StringUtil.GetString(Context.ClientPage.ServerProperties["#pipelineJob"]));
                Job job = JobManager.GetJob(this.jobHandle);
                if (job == null)
                    return;
                if (args.HasResult)
                {
                    var result = HttpContext.Current.Session[args.Result];
                    HttpContext.Current.Session.Remove(args.Result);
                    job.MessageQueue.PutResult(result);
                }
                else
                    job.MessageQueue.PutResult(null);
            }
        }

    }
}