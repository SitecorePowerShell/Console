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
        private static int _powerShellVersionMajor;
        

        public static IEnumerable<string> FindMatches(ScriptSession session, string command)
        {
            if (_powerShellVersionMajor == 0)
            {
                _powerShellVersionMajor = (int)session.ExecuteScriptPart("$PSVersionTable.PSVersion.Major", false, true)[0];
            }

            switch (_powerShellVersionMajor)
            {
                case (2):
                    return FindMatches2(session, command);
                case (3):
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
                @"function ScPsTabExpansionHelper( [string] $inputScript, [int]$cursorColumn ){ TabExpansion2 $inputScript $cursorColumn |% { $_.CompletionMatches } |% { ""$($_.ResultType)|$($_.CompletionText)"" } }";
            session.ExecuteScriptPart(TabExpansionHelper);

            var teCmd = new Command("ScPsTabExpansionHelper");
            teCmd.Parameters.Add("inputScript", command);
            teCmd.Parameters.Add("cursorColumn", command.Length);

            string[] teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>().ToArray();
            var result = new List<string>();

            WrapResults3(truncatedCommand, teResult, result);
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
            var isAbsolute = splitPathResult != null && (bool)splitPathResult;

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

        public static char[] wrapChars = { ' ', '$', '(', ')', '{', '}', '%', '@', '|' };
        public static void WrapResults(string truncatedCommand, IEnumerable<string> tabExpandedResults, List<string> results)
        {
            if (!string.IsNullOrEmpty(truncatedCommand) && !truncatedCommand.EndsWith(" "))
            {
                truncatedCommand += " ";
            }
            results.AddRange(tabExpandedResults.Select(l =>
                l.IndexOfAny(wrapChars) > -1
                    ? string.Format("{0}{1}'{2}'",
                        truncatedCommand,
                        string.IsNullOrEmpty(truncatedCommand) ? string.Empty : " ",
                        l.Trim('\''))
                    : truncatedCommand + l
                ));

        }

        public static void WrapResults3(string truncatedCommand, IEnumerable<string> tabExpandedResults, List<string> results)
        {
            var truncatedCommandTail = (!string.IsNullOrEmpty(truncatedCommand) && !truncatedCommand.EndsWith(" "))
                ? " " : string.Empty;
            results.AddRange(tabExpandedResults.Select(l =>
            {
                var split = l.Split('|');
                var type = split[0];
                var content = split[1];
                switch (type)
                {
                    case ("Variable"):
                    case ("ParameterName"):
                    case ("Command"):
                        return string.Format("{0}{1}{2}",
                            truncatedCommand, truncatedCommandTail, content);
                    case ("Property"):
                    case ("Method"):
                        return truncatedCommand.EndsWith("]")
                            ? string.Format("{0}{1}", truncatedCommand, content)
                            : string.Format("{0}.{1}", truncatedCommand.Trim('.'), content);
                    default:
                        return string.Format("{0}{1}{2}",
                            truncatedCommand, truncatedCommandTail,
                            content.StartsWith("& '") ? content.Substring(2) : content);
                }
            }));

        }

        private static string TruncatedCommand(string command, out string lastToken)
        {
            Collection<PSParseError> errors;
            Collection<PSToken> tokens = PSParser.Tokenize(command, out errors);
            string truncatedCommand = string.Empty;
            lastToken = string.Empty;
            switch (tokens.Count)
            {
                case (0):
                    break;
                case (1):
                    PSToken lastPsToken = tokens.Last();
                    if (lastPsToken.Type != PSTokenType.Variable && lastPsToken.Type != PSTokenType.Command)
                    {
                        truncatedCommand = command;
                    }
                    break;
                default:
                    lastPsToken = tokens.Last();
                    lastToken = lastPsToken.Content;
                    command = command.TrimEnd(' ');
                    truncatedCommand = command.Substring(0, command.Length - lastToken.Length);
                    break;
            }
            return truncatedCommand;
        }
    }
}