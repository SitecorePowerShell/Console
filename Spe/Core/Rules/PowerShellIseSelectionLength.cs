using System.Collections.Generic;
using Sitecore.Diagnostics;
using Sitecore.Rules;
using Sitecore.Rules.Conditions;
using Spe.Core.Diagnostics;

namespace Spe.Core.Rules
{
    public class PowerShellIseSelectionLength<T> : OperatorCondition<T> where T : RuleContext
    {

        private enum Measured
        {
            Script,
            Selection
        };

        // Properties
        public string DesiredLength { get; set; }
        public string MeasuredLength { get; set; }
        private static readonly Dictionary<string, Measured> MeasuredLengths;

        static PowerShellIseSelectionLength()
        {
            Dictionary<string, Measured> dictionary = new Dictionary<string, Measured>
            {
                {"{83DD228B-D7BD-4DE2-B858-DEC59CC06ADF}", Measured.Selection},
                {"{22BE997D-B182-4C9B-888B-FB543D79E7BE}", Measured.Script}
            };
            MeasuredLengths = dictionary;
        }

        // Methods
        protected override bool Execute(T ruleContext)
        {
            Assert.ArgumentNotNull(ruleContext, "ruleContext");

            var measuredLength = MeasuredLengths[MeasuredLength];
            string lengthStr;
            lengthStr = measuredLength == Measured.Script ? ruleContext.Parameters["scriptLength"].ToString() : ruleContext.Parameters["selectionLength"].ToString();

            if (string.IsNullOrEmpty(lengthStr))
            {
                return false;
            }

            if (!int.TryParse(lengthStr, out var length))
            {
                PowerShellLog.Debug("Invalid script length: " + MeasuredLength);
                return false;
            }

            int desiredLength;
            if (!int.TryParse(DesiredLength, out desiredLength))
            {
                PowerShellLog.Debug("Wrong script length definition: " + DesiredLength);
                return false;
            }

            switch (GetOperator())
            {
                case ConditionOperator.Equal:
                    return (length == desiredLength);

                case ConditionOperator.GreaterThanOrEqual:
                    return (length >= desiredLength);

                case ConditionOperator.GreaterThan:
                    return (length > desiredLength);

                case ConditionOperator.LessThanOrEqual:
                    return (length <= desiredLength);

                case ConditionOperator.LessThan:
                    return (length < desiredLength);

                case ConditionOperator.NotEqual:
                    return (length >= desiredLength);
            }
            return false;
        }
    }
}