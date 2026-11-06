using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data.Managers;

namespace Spe.Core.Validation
{
    public class MiscAutocompleteSets
    {
        private static readonly string[] standardItemTypeCompleters =
        {
            "File",
            "Directory",
            "SymbolicLink",
            "Junction",
            "HardLink"
        };

        public static Dictionary<string, string> completers { get; } = new Dictionary<string, string>
        {
            ["Get-Item:Database"] = "[Spe.Commands.BaseLanguageAgnosticItemCommand]::Databases",
            ["Get-ChildItem:Database"] = "[Spe.Commands.BaseLanguageAgnosticItemCommand]::Databases",
            ["Get-Item:Language"] = "[Spe.Commands.BaseItemCommand]::Cultures",
            ["Get-ChildItem:Language"] = "[Spe.Commands.BaseItemCommand]::Cultures",
            ["New-Item:Language"] = "[Spe.Commands.BaseItemCommand]::Cultures",
            ["New-Item:ItemType"] = "[Spe.Core.Validation.MiscAutocompleteSets]::Templates",
            ["Invoke-Script:Path"] = "[Spe.Commands.Session.InvokeScriptCommand]::ScriptPathsForCompletion() | ForEach-Object { \"'$_'\" }",
            ["Invoke-Script:FullName"] = "[Spe.Commands.Session.InvokeScriptCommand]::ScriptPathsForCompletion() | ForEach-Object { \"'$_'\" }",
            ["Invoke-Script:FileName"] = "[Spe.Commands.Session.InvokeScriptCommand]::ScriptPathsForCompletion() | ForEach-Object { \"'$_'\" }",
        };

        public static Dictionary<string, string> Completers => completers;

        public static string[] Templates
        {
            get
            {
                var result = new List<string>(standardItemTypeCompleters);
                result.AddRange(TemplateManager.GetTemplates(Factory.GetDatabase("master"))
                        .Select(template => "\""+template.Value.FullName+ "\"").OrderBy(a => a));
                return result.ToArray();
            }
        }
    }
}