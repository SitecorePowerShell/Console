using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class PromptMessage : BasePipelineMessage
    {
        private readonly string defaultValue;
        private readonly int maxLength;
        private readonly bool simpleRun;
        private readonly string text;
        private readonly string validation;
        private readonly string validationText;

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
        ///     Shows a confirmation dialog.
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