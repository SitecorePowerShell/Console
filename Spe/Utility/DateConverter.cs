using System;
using Sitecore;
using Spe.Abstractions.VersionDecoupling.Interfaces;

namespace Spe.Utility
{
    class DateConverter : IDateConverter
    {
        public DateTime ToServerTime(DateTime timeToConvert)
        {
            return DateUtil.ToServerTime(timeToConvert);
        }
    }
}
