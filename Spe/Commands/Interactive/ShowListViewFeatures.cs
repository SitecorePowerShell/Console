using System;

namespace Spe.Commands.Interactive
{
    [Flags]
    public enum ShowListViewFeatures
    {
        None = 0,
        SharedExport = 1,
        Filter = 2,
        PagingAlways = 4,
        SharedActions = 8,
        StatusBar = 32,
        All = SharedExport | Filter | PagingAlways | SharedActions | StatusBar,
    }
}