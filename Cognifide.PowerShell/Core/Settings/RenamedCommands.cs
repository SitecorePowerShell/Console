using System.Collections.Generic;

namespace Cognifide.PowerShell.Core.Settings
{
    public static class RenamedCommands
    {
        public static Dictionary<string, string> Aliases = new Dictionary<string, string>
        {
            {"Export-Item", "Serialize-Item"},
            {"Import-Item", "Deserialize-Item"},
            {"Invoke-Script", "Execute-Script"},
            {"Invoke-ShellCommand", "Execute-ShellCommand"},
            {"Invoke-Workflow", "Execute-Workflow"},
            {"Install-Package", "Import-Package"},
            {"Initialize-Item", "Wrap-Item"},
            {"Send-File", "Download-File"},
            {"Initialize-SearchIndex", "Rebuild-SearchIndex"}
        };


    }
}
