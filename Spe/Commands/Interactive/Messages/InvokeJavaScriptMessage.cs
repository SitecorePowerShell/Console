using System;
using Sitecore;
using Sitecore.Jobs.AsyncUI;

namespace Spe.Commands.Interactive.Messages
{
    [Serializable]
    public class InvokeJavaScriptMessage : BasePipelineMessage
    {
        private readonly string script;

        public InvokeJavaScriptMessage(string script)
        {
            this.script = script;
        }

        /// <summary>
        ///     Shows a confirmation dialog.
        /// </summary>
        protected override void ShowUI()
        {
                Context.ClientPage.ClientResponse.Eval(script);
        }
    }
}