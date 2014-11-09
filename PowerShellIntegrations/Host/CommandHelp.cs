using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class CommandHelp
    {
        public static IEnumerable<string> GetHelp(ScriptSession session, string command)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            PSToken lastPsToken = tokens.Where(t => t.Type == PSTokenType.Command).LastOrDefault();
            if (lastPsToken != null)
            {
                session.Output.Clear();
                string lastToken = lastPsToken.Content;
                session.SetVariable("helpFor", lastToken);
                Item scriptItem =
                    Database.GetDatabase(ApplicationSettings.ScriptLibraryDb)
                        .GetItem(ScriptLibrary.Path + "Internal/Context Help/Command Help");
                session.ExecuteScriptPart(scriptItem["script"], true, true);
                var sb = new StringBuilder("<div id=\"HelpClose\">x</div>");
                if (session.Output.Count == 0 || session.Output[0].LineType == OutputLineType.Error)
                {
                    return new[]
                    {
                        "<div class='ps-help-command-name'>&nbsp;</div><div class='ps-help-header' align='center'>No Command in line or help information found</div><div class='ps-help-parameter' align='center'>Cannot provide help in this context.</div>"
                    };
                }
                session.Output.ForEach(l => sb.Append(l.Text));
                session.Output.Clear();
                var result = new[] {sb.ToString()};
                return result;
            }
            return new[] {"No Command in line found - cannot provide help in this context."};
        }
    }
}