using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Interactive.Messages
{
    [Serializable]
    public class PromptMessage : BasePipelineMessage, IMessage
    {
        private readonly string text;
        private readonly string defaultValue;
        private readonly string validation;
        private readonly string validationText;
        private readonly int maxLength;
        private readonly bool simpleRun;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Sitecore.Jobs.AsyncUI.ConfirmMessage"/> class.
        /// 
        /// </summary>
        /// <param name="message">The message.</param>
        public PromptMessage(string text, string defaultValue, string validation, string validationText, int maxLength)
        {
            this.text = text;
            this.defaultValue = defaultValue;
            this.validation = validation;
            this.validationText = validationText;
            this.maxLength = maxLength;
            simpleRun = false;
        }

        public PromptMessage(string text, string defaultValue)
        {
            this.text = text;
            this.defaultValue = defaultValue;
            simpleRun = true;
        }

        /// <summary>
        /// Shows a confirmation dialog.
        /// 
        /// </summary>
        protected override void ShowUI()
        {
            if (simpleRun)
            {
                Context.ClientPage.ClientResponse.Input(text, defaultValue);
            }
            else
            {
                Context.ClientPage.ClientResponse.Input(text, defaultValue, validation, validationText, maxLength);
            }
        }
    }
}