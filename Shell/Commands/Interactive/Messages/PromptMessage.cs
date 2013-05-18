using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

namespace Cognifide.PowerShell.Shell.Commands.Interactive.Messages
{
    [Serializable]
    public class PromptMessage : BasePipelineMessage, IMessage
    {
        private string text;
        private string defaultValue;
        private string validation;
        private string validationText;
        private int maxLength;
        private bool simpleRun;

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