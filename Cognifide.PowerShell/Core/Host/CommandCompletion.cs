using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.Host
{
    public static class CommandCompletion
    {
        private static int _powerShellVersionMajor;

        private static readonly string[] dbNames =
            Factory.GetDatabaseNames().ToList().ConvertAll(db => db.ToLower()).ToArray();

        public static char[] wrapChars = {' ', '$', '(', ')', '{', '}', '%', '@', '|'};

        public static IEnumerable<string> FindMatches(ScriptSession session, string command, bool aceResponse)
        {
            if (_powerShellVersionMajor == 0)
            {
                _powerShellVersionMajor =
                    (int) session.ExecuteScriptPart("$PSVersionTable.PSVersion.Major", false, true)[0];
            }

            switch (_powerShellVersionMajor)
            {
                case (1):
                case (2):
                    return FindMatches2(session, command, aceResponse);
                default:
                    return FindMatches3(session, command, aceResponse);
            }
        }

        public static string GetPrefix(ScriptSession session, string command)
        {
            string lastToken;
            TruncatedCommand(session, command, out lastToken);
            return string.IsNullOrEmpty(lastToken) ? string.Empty : lastToken;
        }

        public static IEnumerable<string> FindMatches3(ScriptSession session, string command, bool aceResponse)
        {
            string lastToken;
            var truncatedCommand = TruncatedCommand3(session, command, out lastToken);
            Hashtable options = null;//session.GetVariable("options") as Hashtable;
            Hashtable completers = null;
            if (options == null)
            {
                options = new Hashtable(1);
            }

            if (options.ContainsKey("CustomArgumentCompleters"))
            {
                completers = options["CustomArgumentCompleters"] as Hashtable;
            }
            if (completers == null)
            {
                completers = new Hashtable(CognifideSitecorePowerShellSnapIn.Completers.Count);
                options.Add("CustomArgumentCompleters", completers);
            }

            foreach (var miscCompleter in MiscAutocompleteSets.Completers)
            {
                AddCompleter(session, completers, miscCompleter);
            }
            foreach (var completer in CognifideSitecorePowerShellSnapIn.Completers)
            {
                AddCompleter(session, completers, completer);
            }
            session.SetVariable("options",options);

            const string TabExpansionHelper =
                @"function ScPsTabExpansionHelper( [string] $inputScript, [int]$cursorColumn ){ TabExpansion2 $inputScript $cursorColumn -Options $options |% { $_.CompletionMatches } |% { ""$($_.ResultType)|$($_.CompletionText)"" } }";
            session.ExecuteScriptPart(TabExpansionHelper);

            var teCmd = new Command("ScPsTabExpansionHelper");
            teCmd.Parameters.Add("inputScript", command);
            teCmd.Parameters.Add("cursorColumn", command.Length);
            teCmd.Parameters.Add("options", options);

            var teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>().ToArray();
            var result = new List<string>();

            WrapResults3(truncatedCommand, teResult, result, aceResponse);
            return result;
        }

        private static void AddCompleter(ScriptSession session, Hashtable completers, KeyValuePair<string, string> completer)
        {
            if (!completers.ContainsKey(completer.Key))
            {
                completers.Add(completer.Key,
                    session.GetScriptBlock(
                        "param($commandName, $parameterName, $wordToComplete, $commandAst, $fakeBoundParameter) \r\n " +
                        completer.Value +
                        " | ForEach-Object { New-Object System.Management.Automation.CompletionResult $_, $_, 'ParameterValue', $_ }"
                        ));
            }
        }

        public static IEnumerable<string> FindMatches2(ScriptSession session, string command, bool aceResponse)
        {
            string lastToken;
            var truncatedCommand = TruncatedCommand2(command, out lastToken);

            var teCmd = new Command("TabExpansion");
            teCmd.Parameters.Add("line", command);
            teCmd.Parameters.Add("lastWord", lastToken);
            var teResult = session.ExecuteCommand(teCmd, false, true).Cast<string>();

            var result = new List<string>();
            WrapResults(truncatedCommand, teResult, result, aceResponse);

            var splitPathResult =
                (session.ExecuteScriptPart(string.Format("Split-Path \"{0}*\" -IsAbsolute", lastToken), false, true)
                    .FirstOrDefault());
            var isAbsolute = splitPathResult != null && (bool) splitPathResult;

            if (isAbsolute)
            {
                var commandLine = string.Format("Resolve-Path \"{0}*\"", lastToken);
                var psResults = session.ExecuteScriptPart(commandLine, false, true);
                var rpResult = psResults.Cast<PathInfo>().Select(p => p.Path);
                WrapResults(truncatedCommand, rpResult, result, aceResponse);
            }
            else
            {
                var commandLine = string.Format("Resolve-Path \"{0}*\" -Relative", lastToken);
                var rpResult = session.ExecuteScriptPart(commandLine, false, true).Cast<string>();
                WrapResults(truncatedCommand, rpResult, result, aceResponse);
            }

            return result;
        }

        public static void WrapResults(string truncatedCommand, IEnumerable<string> tabExpandedResults,
            List<string> results, bool aceResponse)
        {
            if (!string.IsNullOrEmpty(truncatedCommand) && !truncatedCommand.EndsWith(" "))
            {
                truncatedCommand += " ";
            }
            results.AddRange(tabExpandedResults.Select(l =>
            {
                if (aceResponse)
                {
                    return "Server-side|" + l;
                }
                return l.IndexOfAny(wrapChars) > -1
                    ? string.Format("{0}{1}'{2}'",
                        truncatedCommand,
                        string.IsNullOrEmpty(truncatedCommand) ? string.Empty : " ",
                        l.Trim('\''))
                    : truncatedCommand + l;
            }
                ));
        }

        public static void WrapResults3(string truncatedCommand, IEnumerable<string> tabExpandedResults,
            List<string> results, bool aceResponse)
        {
            var truncatedCommandTail = (!string.IsNullOrEmpty(truncatedCommand) && !truncatedCommand.EndsWith(" "))
                ? " "
                : string.Empty;
            results.AddRange(tabExpandedResults.Select(l =>
            {
                var split = l.Split('|');
                var type = split[0];
                var content = split[1];

                if (aceResponse)
                {
                    switch (type)
                    {
                        case ("ProviderItem"):
                        case ("ProviderContainer"):
                            content = content.Trim('\'', ' ', '&', '"');
                            return IsSitecoreItem(content)
                                ? string.Format("Item|{0}|{1}", content.Split('\\').Last(), content)
                                : string.Format("{0}|{1}|{2}", type, content.Split('\\').Last(), content);
                        case ("ParameterName"):
                            return string.Format("Parameter|{0}", content);
                        default:
                            return l;
                    }
                }
                switch (type)
                {
                    case ("Variable"):
                    case ("ParameterName"):
                    case ("Command"):
                        return string.Format("{0}{1}{2}",
                            truncatedCommand, truncatedCommandTail, content);
                    case ("Property"):
                    case ("Method"):
                        return string.Format("{0}{1}", truncatedCommand, content);
                    default:
                        return string.Format("{0}{1}{2}",
                            truncatedCommand, truncatedCommandTail,
                            content.StartsWith("& '") ? content.Substring(2) : content);
                }
            }));
        }

        private static bool IsSitecoreItem(string path)
        {
            if (!string.IsNullOrEmpty(path) && path.IndexOf(':') > 0)
            {
                var db = path.Split(':')[0].ToLower();
                return dbNames.Contains(db);
            }
            return false;
        }

        private static void TruncatedCommand(ScriptSession session, string command, out string lastToken)
        {
            {
                if (_powerShellVersionMajor == 0)
                {
                    _powerShellVersionMajor =
                        (int) session.ExecuteScriptPart("$PSVersionTable.PSVersion.Major", false, true)[0];
                }

                switch (_powerShellVersionMajor)
                {
                    case (2):
                        TruncatedCommand2(command, out lastToken);
                        break;
                    case (3):
                    case (4):
                        TruncatedCommand3(session, command, out lastToken);
                        break;
                    default:
                        lastToken = string.Empty;
                        break;
                }
            }
        }

        private static string TruncatedCommand3(ScriptSession session, string command, out string lastToken)
        {
            const string TabExpansionHelper =
                @"function ScPsReplacementIndex( [string] $inputScript, [int]$cursorColumn ){ TabExpansion2 $inputScript $cursorColumn |% { $_.ReplacementIndex } }";
            session.ExecuteScriptPart(TabExpansionHelper);

            var teCmd = new Command("ScPsReplacementIndex");
            teCmd.Parameters.Add("inputScript", command);
            teCmd.Parameters.Add("cursorColumn", command.Length);

            var teResult = session.ExecuteCommand(teCmd, false, true).Cast<int>().First();

            lastToken = command.Substring(teResult);
            return command.Substring(0, teResult);
        }

        private static string TruncatedCommand2(string command, out string lastToken)
        {
            Collection<PSParseError> errors;
            var tokens = PSParser.Tokenize(command, out errors);
            var truncatedCommand = string.Empty;
            lastToken = string.Empty;
            switch (tokens.Count)
            {
                case (0):
                    break;
                default:
                    var lastPsToken = tokens.Last();
                    var start = lastPsToken.Start;
                    if ((lastPsToken.Content == "\\" || lastPsToken.Content == "/") && tokens.Count > 1 &
                        tokens[tokens.Count - 2].Type == PSTokenType.String)
                    {
                        start = tokens[tokens.Count - 2].Start;
                    }
                    lastToken = command.Substring(start, lastPsToken.EndColumn - 1 - start);
                    //command = command.TrimEnd(' ');
                    if (lastPsToken.Type == PSTokenType.Operator && lastPsToken.Content != "-")
                    {
                        truncatedCommand = command;
                        lastToken = string.Empty;
                    }
                    else
                    {
                        truncatedCommand = command.Substring(0, command.Length - lastToken.Length);
                    }
                    break;
            }
            return truncatedCommand;
        }
    }
}