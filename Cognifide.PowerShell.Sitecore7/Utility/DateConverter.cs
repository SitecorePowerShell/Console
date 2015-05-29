using System;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;

namespace Cognifide.PowerShell.VersionSpecific.Utility
{
    class DateConverter : IDateConverter
    {
        public DateTime ToServerTime(DateTime timeToConvert)
        {
            return timeToConvert;
        }
    }
}
