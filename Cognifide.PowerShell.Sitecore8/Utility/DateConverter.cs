using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;

namespace Cognifide.PowerShell.Sitecore8.Utility
{
    class DateConverter : IDateConverter
    {
        public DateTime ToServerTime(DateTime timeToConvert)
        {
            return DateUtil.ToServerTime(timeToConvert);
        }
    }
}
