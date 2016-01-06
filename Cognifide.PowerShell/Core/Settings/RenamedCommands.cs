using System.Collections.Generic;
using System.Text;

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
            {"Initialize-SearchIndex", "Rebuild-SearchIndex"},
            {"Initialize-SearchIndexItem", "Rebuild-SearchIndexItem"},
            {"Add-ItemVersion", "Add-ItemLanguage"},
            {"Remove-ItemVersion", "Remove-ItemLanguage"}
        };

        private static string aliasSetupScript;

        public static string AliasSetupScript
        {
            get
            {
                if (aliasSetupScript == null)
                {
                    var sb = new StringBuilder(2048);
                    foreach (var rename in Aliases)
                    {
                        sb.AppendFormat(
                            "New-Alias {1} {0} -Description '{1}->{0}'-Scope Global -Option AllScope,Constant\n",
                            rename.Key,
                            rename.Value);
                    }
                    aliasSetupScript = sb.ToString();
                }
                return aliasSetupScript;
            }
        }

    }
}
