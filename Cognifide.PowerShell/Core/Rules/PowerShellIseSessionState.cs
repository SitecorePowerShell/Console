using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.CodeDom.Scripts;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellIseSessionState<T> : OperatorCondition<T> where T : RuleContext
    {
        private enum SessionState
        {
            Saved,
            Modified
        };

        // Properties
        public string State { get; set; }
        private static readonly Dictionary<string, SessionState> SessionStates;

        static PowerShellIseSessionState()
        {
            Dictionary<string, SessionState> dictionary = new Dictionary<string, SessionState>
            {
                {"{9B141D4F-BCC7-4579-A108-192B955C3539}", SessionState.Saved},
                {"{CA052047-96B6-4802-A58E-AA353757B585}", SessionState.Modified}
            };
            SessionStates = dictionary;
        }

        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            var sessionState = SessionStates[State];
            switch (sessionState)
            {
                case (SessionState.Saved):
                    return ruleContext.Item != null;
                case (SessionState.Modified):
                    bool modified;
                    return Boolean.TryParse(ruleContext.Parameters["modified"] as string, out modified) && modified;
                default:
                    return false;
            }
        }
    }
}