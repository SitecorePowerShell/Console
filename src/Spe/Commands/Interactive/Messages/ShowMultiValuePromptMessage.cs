using System;
using System.Collections;
using System.Management.Automation;
using Sitecore;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;
using Spe.Core.Host;

namespace Spe.Commands.Interactive.Messages
{
    [Serializable]
    public class ShowMultiValuePromptMessage : BasePipelineMessageWithResult
    {
        [NonSerialized] private readonly object[] parameters;

        public ShowMultiValuePromptMessage(object[] parameters, string width, string height, string title,
            string description, string icon, string okButtonName, string cancelButtonName, bool showHints, ScriptBlock validator,
            Hashtable validatorParameters, string sessionKey) : base()
        {
            this.parameters = parameters;
            Width = width ?? string.Empty;
            Height = height ?? string.Empty;
            Title = title ?? string.Empty;
            Icon = icon ?? string.Empty;
            OkButtonName = okButtonName ?? string.Empty;
            CancelButtonName = cancelButtonName ?? string.Empty;
            Description = description ?? string.Empty;
            ShowHints = showHints;
            Validator = validator;
            ValidatorParameters = validatorParameters;
            SessionKey = sessionKey;
        }

        public object[] Parameters => parameters;

        public string Width { get; }
        public string Height { get; }
        public string Title { get; }
        public string Icon { get; }
        public string Description { get; }
        public string CancelButtonName { get; }
        public string OkButtonName { get; }
        public bool ShowHints { get; }
        public ScriptBlock Validator { get; }
        public Hashtable ValidatorParameters { get; }
        public string SessionKey { get; }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
            var session = ScriptSessionManager.GetSession(SessionKey);
            session.DialogStack.Push(this);
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellMultiValuePrompt"));
            urlString.Add("sid", SessionKey);
            SheerResponse.ShowModalDialog(urlString.ToString(), Width, Height, "", true);
        }

        protected override object ProcessResult(bool hasResult, string sig)
        {
            var session = ScriptSessionManager.GetSession(SessionKey);
            session.DialogStack.Pop();
            if (hasResult)
            {
                var result = session.DialogResults;
                session.DialogResults = null;
                return result;
            }
            return null;
        }
    }
}