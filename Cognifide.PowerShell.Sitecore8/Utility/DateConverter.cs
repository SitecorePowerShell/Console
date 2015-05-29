using System;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;

namespace Cognifide.PowerShell.VersionSpecific.Utility
{
    class DateConverter : IDateConverter
    {
        public DateTime ToServerTime(DateTime timeToConvert)
        {
            return DateUtil.ToServerTime(timeToConvert);
        }
    }
}
