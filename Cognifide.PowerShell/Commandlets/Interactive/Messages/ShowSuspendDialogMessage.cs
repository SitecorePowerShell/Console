using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Core.Host;

namespace Cognifide.PowerShell.Commandlets.Interactive.Messages
{
    [Serializable]
    public class ShowSuspendDialogMessage : ShowModalDialogPsMessage
    {
        public string SessionKey { get; }
        public ShowSuspendDialogMessage(string sessionKey, string url, string width, string height, Hashtable handleParams) : base(url, width, height, handleParams)
        {
            SessionKey = sessionKey;
        }

        protected override object ProcessResult(bool hasResult, string result)
        {
            if (ScriptSessionManager.GetSessionIfExists(SessionKey) is ScriptSession session)
            {
                session.Host.EndNestedPromptSuspension();
            }
            return base.ProcessResult(hasResult, result);
        }

    }
}