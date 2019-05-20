using System;

namespace Cognifide.PowerShell.Abstractions.VersionDecoupling.Interfaces
{
    public interface IDateConverter
    {
        DateTime ToServerTime(DateTime timeToConvert);
    }
}
