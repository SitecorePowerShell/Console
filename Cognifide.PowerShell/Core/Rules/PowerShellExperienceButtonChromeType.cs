using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.CodeDom.Scripts;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;

namespace Cognifide.PowerShell.Core.Rules
{
    public class PowerShellExperienceButtonChromeType<T> : OperatorCondition<T> where T : RuleContext
    {

        private enum ChromeObjectType
        {
            PlaceHolder,
            Field,
            Rendering
        };

        // Properties
        public string ChromeType { get; set; }

        private static readonly Dictionary<string, ChromeObjectType> ChromeObjectTypes;

        static PowerShellExperienceButtonChromeType()
        {
            Dictionary<string, ChromeObjectType> dictionary = new Dictionary<string, ChromeObjectType>
            {
                {"{4C6068C8-E969-4DFD-B22C-33E820D20651}", ChromeObjectType.PlaceHolder},
                {"{F4E27AEB-AA6C-4B36-B8B5-72648F47BC98}", ChromeObjectType.Field},
                {"{041A8A44-DD56-42AB-B4E3-579845669252}", ChromeObjectType.Rendering}
            };
            ChromeObjectTypes = dictionary;
        }

        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            var desiredObjectType = ChromeObjectTypes[ChromeType].ToString();
            var actualObjectType = ruleContext.Parameters.ContainsKey("ChromeType")
                ? ruleContext.Parameters["ChromeType"].ToString() : "!@#UNIQUE#@!";
            return string.Equals(desiredObjectType, actualObjectType, StringComparison.OrdinalIgnoreCase);
        }
    }
}