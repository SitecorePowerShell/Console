using System;
using System.Management.Automation;
using System.Web;
using Cognifide.PowerShell.Client.Applications;
using Sitecore;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using JobManager = Sitecore.Jobs.JobManager;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowMultiValuePromptMessage : BasePipelineMessageWithResult
    {
        [NonSerialized] private readonly object[] parameters;

        public ShowMultiValuePromptMessage(object[] parameters, string width, string height, string title,
            string description, string okButtonName, string cancelButtonName, bool showHints, ScriptBlock validator) : base()
        {
            this.parameters = parameters;
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
            ShowHints = showHints;
            Validator = validator;
        }

        public object[] Parameters
        {
            get { return parameters; }
        }

        public string Width { get; private set; }
        public string Height { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string CancelButtonName { get; private set; }
        public string OkButtonName { get; private set; }
        public bool ShowHints { get; set; }
        public ScriptBlock Validator { get; private set; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var resultSig = Guid.NewGuid().ToString();
            HttpContext.Current.Cache[resultSig] = this;
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellMultiValuePrompt"));
            urlString.Add("sid", resultSig);
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height, "", true);
        }

        protected override object ProcessResult(bool hasResult, string sig)
        {
            if (hasResult)
            {
                var result = HttpContext.Current.Cache[sig];
                HttpContext.Current.Cache.Remove(sig);
                return result;
            }
            return null;
        }
    }
}