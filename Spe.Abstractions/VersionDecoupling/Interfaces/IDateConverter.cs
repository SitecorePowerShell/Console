using System;

namespace Spe.Abstractions.VersionDecoupling.Interfaces
{
    public interface IDateConverter
    {
        DateTime ToServerTime(DateTime timeToConvert);
    }
}