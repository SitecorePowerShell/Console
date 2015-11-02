using System;

namespace Cognifide.PowerShell.Commandlets.Interactive
{
    [Flags]
    public enum HideListViewFeatures
    {
        None = 0,
        AllExport = 1,
        NonSpecificExport = 2,
        Filter = 4,
        PagingWhenNotNeeded = 8,
        AllActions = 16,
        NonSpecificActions = 32,
        StatusBar = 64
    }
}