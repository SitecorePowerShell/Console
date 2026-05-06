using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal class ApiScriptCollection : ConcurrentDictionary<string, SortedDictionary<string, ApiScript>> {}
}
