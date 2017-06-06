using System;
using System.Collections;
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
            string description, string okButtonName, string cancelButtonName, bool showHints, ScriptBlock validator,
            Hashtable validatorParameters) : base()
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
            ValidatorParameters = validatorParameters;
        }

        public object[] Parameters => parameters;

        public string Width { get; }
        public string Height { get; }
        public string Title { get; }
        public string Description { get; }
        public string CancelButtonName { get; }
        public string OkButtonName { get; }
        public bool ShowHints { get; }
        public ScriptBlock Validator { get; }
        public Hashtable ValidatorParameters { get; }

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