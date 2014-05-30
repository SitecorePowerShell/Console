using System;
using System.Web;
using Cognifide.PowerShell.SitecoreIntegrations.Applications;
using Sitecore;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Framework;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowMultiValuePromptMessage : IMessage, IMessageWithResult
    {
        public object[] Parameters { get; private set; }
        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string CancelButtonName { get; private set; }
        public string OkButtonName { get; private set; }
        public bool ShowHints { get; set; }

        private Handle jobHandle;
        public MessageQueue MessageQueue { get; private set; }
        public object Result { get; private set; }

        public ShowMultiValuePromptMessage(object[] parameters, string width, string height, string title,
            string description, string okButtonName, string cancelButtonName, bool showHints)
        {
            MessageQueue = new MessageQueue();
            if (JobContext.IsJob)
            {
                jobHandle = JobContext.JobHandle;
            }
            Parameters = parameters;
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
            ShowHints = showHints;
        }



        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected virtual void ShowUI()
        {
            string resultSig = Guid.NewGuid().ToString();
            if (Context.ClientPage.CodeBeside is IPowerShellRunner)
            {
                (Context.ClientPage.CodeBeside as IPowerShellRunner).Monitor.Active = false;
            }

            HttpContext.Current.Session[resultSig] = Parameters;
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellMultiValuePrompt"));
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
            urlString.Add("sh", ShowHints ? "1" : "0");
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
                if (args.HasResult)
                {
                    object result = HttpContext.Current.Session[args.Result];
                    HttpContext.Current.Session.Remove(args.Result);
                    Result = result;
                }
                else
                {
                    Result = null;
                }


                string strJobId = StringUtil.GetString(Context.ClientPage.ServerProperties["#pipelineJob"]);
                if (!String.IsNullOrEmpty(strJobId))
                {
                    jobHandle = Handle.Parse(strJobId);
                    Job job = JobManager.GetJob(jobHandle);
                    if (job != null)
                    {
                        job.MessageQueue.PutResult(Result);
                    }
                }
                else
                {
                    MessageQueue.PutResult(Result);
                }

                if (Context.ClientPage.CodeBeside is IPowerShellRunner)
                {
                    (Context.ClientPage.CodeBeside as IPowerShellRunner).Monitor.Active = true;
                }
            }
        }
    }
}