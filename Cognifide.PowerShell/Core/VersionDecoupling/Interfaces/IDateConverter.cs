using System;

namespace Cognifide.PowerShell.Core.VersionDecoupling.Interfaces
{
    public interface IDateConverter
    {
        DateTime ToServerTime(DateTime timeToConvert);
    }
}
