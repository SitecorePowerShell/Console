using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.SitecoreIntegrations.Applications;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class CommandCompletion
    {
        public static IEnumerable<string> FindMatches(ScriptSession session, string command)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            string lastToken = tokens.Last().Content;
            string truncatedCommand = string.Empty;
            switch (tokens.Count)
            {
                case (0):
                    break;
                case (1):
                    break;
                default:

                    truncatedCommand = tokens.Take(tokens.Count - 1).
                                              Select(
                                                  l =>
                                                  l.Content.Contains(" ")
                                                      ? string.Format("\"{0}\"", l.Content)
                                                      : l.Content)
                                             .
                                              Aggregate((x, y) => x + " " + y) + " ";
                    //command.Substring(0, command.Length - lastToken.Length);
                    break;
            }
            lastToken = lastToken.Trim('"', '\'');

            var teCmd = new Command("TabExpansion");
            teCmd.Parameters.Add("line", command);
            teCmd.Parameters.Add("lastWord", lastToken);
            IEnumerable<string> teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>();

            //IEnumerable<string> teResult = session.ExecuteScriptPart(string.Format("TabExpansion \"{0}\" \"{1}\"", command, lastToken),false,true).Cast<string>();
            List<string> result = teResult.Select(l => truncatedCommand + l).ToList();

            //int prefixFileNameLength = Path.GetFileName(lastToken).Length;
            //string pathPrefix = lastToken.Substring(lastToken.Length - prefixFileNameLength);
            var splitPathResult = (session.ExecuteScriptPart(string.Format("Split-Path \"{0}*\" -IsAbsolute", lastToken), false, true).FirstOrDefault());
            var isAbsolute = splitPathResult != null && (bool) splitPathResult;

            if (isAbsolute)
            {
                string commandLine = string.Format("Resolve-Path \"{0}*\"", lastToken);
                var psResults = session.ExecuteScriptPart(commandLine, false, true);
                IEnumerable<PathInfo> rpResult = psResults.Cast<PathInfo>();
                result.AddRange(rpResult.Select(l => truncatedCommand + l.Path));
            }
            else
            {
                string commandLine = string.Format("Resolve-Path \"{0}*\" -Relative", lastToken);
                IEnumerable<string> rpResult = session.ExecuteScriptPart(commandLine, false, true).Cast<string>();
                result.AddRange(rpResult.Select(l =>
                                                l.Contains(" ")
                                                    ? string.Format("{0} \"{1}\"", truncatedCommand, l)
                                                    : truncatedCommand + l
                                    ));
            }

            return result;
        }
    }
}