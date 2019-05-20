using System;
using Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces;
using Sitecore;

namespace Cognifide.PowerShell.Utility
{
    class DateConverter : IDateConverter
    {
        public DateTime ToServerTime(DateTime timeToConvert)
        {
            return DateUtil.ToServerTime(timeToConvert);
        }
    }
}
