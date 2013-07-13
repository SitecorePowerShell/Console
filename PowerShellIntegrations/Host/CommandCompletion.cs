using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using Cognifide.PowerShell.SitecoreIntegrations.Applications;
using Sitecore.Form.Core.Utility;

namespace Cognifide.PowerShell.PowerShellIntegrations.Host
{
    public static class CommandCompletion
    {
        private static int powershellVersion = 0;

        public static IEnumerable<string> FindMatches(ScriptSession session, string command)
        {
            if (powershellVersion == 0)
            {
                powershellVersion = (bool)session.ExecuteScriptPart("Test-Path Function:\\TabExpansion2", false, true)[0] ? 3 : 2;
            }

            switch (powershellVersion)
            {
                case(2):
                    return FindMatches2(session, command);
                case(3):
                    return FindMatches3(session, command);
                default:
                    return new string[0];
            }
        }

        public static IEnumerable<string> FindMatches3(ScriptSession session, string command)
        {
            string lastToken;
            var truncatedCommand = TruncatedCommand(command, out lastToken);

            string TabExpansionHelper =
                @"function ScPsTabExpansionHelper( [string] $inputScript, [int]$cursorColumn ){ TabExpansion2 $inputScript $cursorColumn |% { $_.CompletionMatches.CompletionText } }";
            session.ExecuteScriptPart(TabExpansionHelper);

            var teCmd = new Command("ScPsTabExpansionHelper");
            teCmd.Parameters.Add("inputScript", command);
            teCmd.Parameters.Add("cursorColumn",command.Length);

            string[] teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>().Select(l=> l.StartsWith("& '") ? l.Substring(3).TrimEnd('\'') : l).ToArray();
            var result = new List<string>();

            WrapResults(truncatedCommand, teResult, result);
            return result;
        }

        public static IEnumerable<string> FindMatches2(ScriptSession session, string command)
        {
            string lastToken;
            var truncatedCommand = TruncatedCommand(command, out lastToken);

            var teCmd = new Command("TabExpansion");
            teCmd.Parameters.Add("line", command);
            teCmd.Parameters.Add("lastWord", lastToken);
            IEnumerable<string> teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>();

            //IEnumerable<string> teResult = session.ExecuteScriptPart(string.Format("TabExpansion \"{0}\" \"{1}\"", command, lastToken),false,true).Cast<string>();
            var result = new List<string>();
            WrapResults(truncatedCommand, teResult, result);

            //int prefixFileNameLength = Path.GetFileName(lastToken).Length;
            //string pathPrefix = lastToken.Substring(lastToken.Length - prefixFileNameLength);
            var splitPathResult = (session.ExecuteScriptPart(string.Format("Split-Path \"{0}*\" -IsAbsolute", lastToken), false, true).FirstOrDefault());
            var isAbsolute = splitPathResult != null && (bool) splitPathResult;

            if (isAbsolute)
            {
                string commandLine = string.Format("Resolve-Path \"{0}*\"", lastToken);
                var psResults = session.ExecuteScriptPart(commandLine, false, true);
                IEnumerable<string> rpResult = psResults.Cast<PathInfo>().Select(p => p.Path);
                //result.AddRange(rpResult.Select(l => truncatedCommand + l.Path));
                WrapResults(truncatedCommand, rpResult, result);
            }
            else
            {
                string commandLine = string.Format("Resolve-Path \"{0}*\" -Relative", lastToken);
                IEnumerable<string> rpResult = session.ExecuteScriptPart(commandLine, false, true).Cast<string>();
                WrapResults(truncatedCommand, rpResult, result);
            }

            return result;
        }

        public static char[] wrapChars = {' ', '$', '(', ')', '{', '}', '%', '@', '|'};
        public static void WrapResults(string truncatedCommand, IEnumerable<string> tabExpandedResults, List<string> results)
        {
            if (!string.IsNullOrEmpty(truncatedCommand))
            {
                truncatedCommand += " ";
            }
            results.AddRange(tabExpandedResults.Select(l =>
                l.IndexOfAny(wrapChars) > -1
                    ? string.Format("{0}{1}'{2}'",
                        truncatedCommand,
                        string.IsNullOrEmpty(truncatedCommand) ? string.Empty: " ",
                        l.Trim('\''))
                    : truncatedCommand + l
                ));

        }

        private static string TruncatedCommand(string command, out string lastToken)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            lastToken = tokens.Last().Content;
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
                    break;
            }
            return truncatedCommand;
        }
    }
}