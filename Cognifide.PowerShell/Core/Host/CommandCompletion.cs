using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using System.Text.RegularExpressions;
using Cognifide.PowerShell.Commandlets;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Cognifide.PowerShell.Core.Validation;
using Sitecore.Configuration;

namespace Cognifide.PowerShell.Core.Host
{
    public static class CommandCompletion
    {

        private static readonly string[] dbNames =
            Factory.GetDatabaseNames().ToList().ConvertAll(db => db.ToLower()).ToArray();

        private static Regex staticFuncRegex = new Regex(@"^\[(?<class>[\w\.]*)\]::(?<method>\w*)\(");
        private static Regex variableFuncRegex = new Regex(@"^(?<expression>\$[\w\.]*)\.(?<method>\w*)\(");
        private static Regex variableRegex = new Regex(@"^(?<variable>\$[\w]*)");
        private static Regex staticExpressionRegex = new Regex(@"^(?<expression>[\[\w\.\:\]]*)\.(?<method>\w*)\(");
        public static char[] wrapChars = {' ', '$', '(', ')', '{', '}', '%', '@', '|'};

        const string TabExpansionHelper =
            @"function ScPsTabExpansionHelper( [string] $inputScript, [int]$cursorColumn , $options){ TabExpansion2 $inputScript $cursorColumn -Options $options |% { $_.CompletionMatches } |% { ""$($_.ResultType)|$($_.CompletionText)"" } }";

        private static Hashtable options;

        public static IEnumerable<string> FindMatches(ScriptSession session, string command, bool aceResponse)
        {
            switch (ScriptSession.PsVersion.Major)
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
            //command = command.Trim();
            var truncatedCommand = TruncatedCommand3(session, command, out lastToken) ?? string.Empty;
            var truncatedLength = truncatedCommand.Length;
            var options = Completers;
            lastToken = lastToken.Trim();
            if (!string.IsNullOrEmpty(lastToken) && lastToken.Trim().StartsWith("["))
            {
                if (lastToken.IndexOf("]", StringComparison.Ordinal) < 0)
                {
                    return CompleteTypes(lastToken, truncatedLength);
                }
                if (staticFuncRegex.IsMatch(lastToken))
                {
                    var matches = staticFuncRegex.Matches(lastToken);
                    var className = matches[0].Groups["class"].Value;
                    var methodName = matches[0].Groups["method"].Value;

                    const BindingFlags bindingFlags =
                        BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;

                    // look in acceelerators first
                    if (!className.Contains('.') && TypeAccelerators.AllAccelerators.ContainsKey(className))
                    {
                        return GetMethodSignatures(TypeAccelerators.AllAccelerators[className], methodName, bindingFlags, truncatedLength);
                    }

                    // check the loaded assemblies
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            var type = assembly
                                .GetExportedTypes()
                                .FirstOrDefault(
                                    aType => aType.FullName.Equals(className, StringComparison.OrdinalIgnoreCase));
                            if (type != null)
                            {
                                return GetMethodSignatures(type, methodName, bindingFlags, truncatedLength);
                            }
                        }
                        catch
                        {
                            // ignore on purpose
                        }
                    }
                }
            }

            if (!string.IsNullOrEmpty(lastToken) && (lastToken.StartsWith("$") || lastToken.StartsWith("[")))
            {
                MatchCollection matches = null;
                var matched = false;
                if (variableFuncRegex.IsMatch(lastToken))
                {
                    matches = variableFuncRegex.Matches(lastToken);
                    matched = true;
                }
                else if (staticExpressionRegex.IsMatch(lastToken))
                {
                    matches = staticExpressionRegex.Matches(lastToken);
                    matched = true;
                }

                if (matched)
                {
                    //var matches = variableFuncRegex.Matches(lastToken);
                    var expression = matches[0].Groups["expression"].Value;
                    var methodName = matches[0].Groups["method"].Value;
                    Type objectType = null;
                    List<object> objectValue;
                    try
                    {
                        if (session.TryInvokeInRunningSession(expression, out objectValue, false))
                        {
                            if (objectValue != null && objectValue.Count > 0)
                            {
                                objectType = objectValue[0].GetType();
                            }
                        }
                    }
                    catch //(Exception ex) - variable may not be found if session does not exist
                    {
                        var varName = variableRegex.Matches(lastToken)[0].Value;
                        var message = $"Variable {varName} not found in session. Execute script first.";
                        return new List<string> {$"Signature|{message}|{truncatedLength}|{message}"};
                    }
                    if (objectType != null)
                    {
                        const BindingFlags bindingFlags =
                            BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance |
                            BindingFlags.FlattenHierarchy;
                        return GetMethodSignatures(objectType, methodName, bindingFlags, truncatedLength);
                    }
                }
            }


            session.TryInvokeInRunningSession(TabExpansionHelper);

            var teCmd = new Command("ScPsTabExpansionHelper");
            teCmd.Parameters.Add("inputScript", command);
            teCmd.Parameters.Add("cursorColumn", command.Length);
            teCmd.Parameters.Add("options", options);

            var teResult = new string[0];

            List<object> results;
            if (session.TryInvokeInRunningSession(teCmd, out results, true))
            {
                teResult = results.Cast<string>().ToArray();
            }
            var result = new List<string>();

            WrapResults3(truncatedCommand, teResult, result, aceResponse);
            return result;
        }

        private static IEnumerable<string> GetMethodSignatures(Type type, string methodName,
            BindingFlags bindingFlags,
            int truncatedLength)
        {

            if (type != null)
            {
                var methods = type.GetMethods(bindingFlags);
                var filtered =
                    methods.Where(
                        method => methodName.Equals(method.Name, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                var signatures =
                    filtered.Select(mi => GetSignature(mi, truncatedLength))
                        .Select(sig => $"Signature|{sig}|{truncatedLength}")
                        .ToList();

                if (signatures.Count == 0)
                {
                    var message = $"<method not found on class [{type.FullName}]>";
                    signatures.Add($"Signature|{message}|{truncatedLength}|{message}");
                }

                return signatures.ToArray();
            }
            return new List<string> {$"Signature|<no parameters>|{truncatedLength}|<no parameters>"};
        }

        private static string GetSignature(MethodInfo mi, int position)
        {
            var param1 = mi.GetParameters()
                .Select(p => $"[{p.ParameterType.FullName}] ${p.Name}")
                .ToArray();
            var param2 = mi.GetParameters()
                .Select(p => $"[{p.ParameterType.Name}] ${p.Name}")
                .ToArray();
            if (param1.Length == 0)
            {
                return $"<no parameters>|{position}|<no parameters>";
            }
            //var signature = $"[{mi.ReturnType.Name}] {mi.Name}({string.Join(",", param)})";
            var signature = string.Join(", ", param2) + $"|{position}|" + string.Join(", ", param1);

            return signature;
        }

        private static IEnumerable<string> CompleteTypes(string completeToken, int position)
        {
            var results = new List<string>();
            completeToken = completeToken.Trim('[', ']');
            var lastDotPosition = completeToken.LastIndexOf('.');
            var hasdot = lastDotPosition > -1;
            var endsWithDot = completeToken.Length == lastDotPosition - 1;
            WildcardPattern nameWildcard;
            WildcardPattern fullWildcard;
            if (hasdot)
            {
                var namespaceToken = completeToken.Substring(0, lastDotPosition);
                var nameToken = completeToken.Substring(lastDotPosition + 1);
                if (endsWithDot)
                {
                    nameWildcard = WildcardUtils.GetWildcardPattern("*");
                }
                else
                {
                    nameWildcard = WildcardUtils.GetWildcardPattern($"{nameToken}*");
                }
                namespaceToken = namespaceToken.Replace(".", "*.");
                fullWildcard = WildcardUtils.GetWildcardPattern($"{namespaceToken}*");
            }
            else
            {
                nameWildcard = WildcardUtils.GetWildcardPattern($"{completeToken}*");
                fullWildcard = WildcardUtils.GetWildcardPattern($"{completeToken}.*");

                //autocomplete accelerators
                var accelerators = TypeAccelerators.AllAccelerators;
                results.AddRange(
                    accelerators.Keys
                        .Where(acc => (nameWildcard.IsMatch(acc)))
                        .Select(
                            type => $"Type|{type} (Accelerator -> {accelerators[type].FullName})|{position}|{type}"));
            }

            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (hasdot)
                    {
                        results.AddRange(
                            assembly.GetExportedTypes()
                                .Where(type => nameWildcard.IsMatch(type.Name) &&
                                               fullWildcard.IsMatch(type.Namespace) &&
                                               !type.Name.Contains('`'))
                                .Select(type => $"Type|{type.Name} ({type.Namespace})|{position}|{type.FullName}"));
                    }
                    else
                    {
                        results.AddRange(
                            assembly.GetExportedTypes()
                                .Where(type => (nameWildcard.IsMatch(type.Name) ||
                                                fullWildcard.IsMatch(type.Namespace)) &&
                                               !type.Name.Contains('`'))
                                .Select(type => $"Type|{type.Name} ({type.Namespace})|{position}|{type.FullName}"));
                    }
                }
                catch //(Exception e)
                {
                    // PowerShellLog.Error("Error enumerating types", e);
                    // Ignoring intentionally... 
                    // This just happens for some assembiles with no consequences to user experience
                }
            }
            results.Sort(StringComparer.OrdinalIgnoreCase);
            return results.ToArray();
        }

        private static Hashtable Completers
        {
            get
            {
                if (options == null)
                {
                    options = new Hashtable(1);
                    Hashtable completers = null;

                    if (options.ContainsKey("CustomArgumentCompleters"))
                    {
                        completers = options["CustomArgumentCompleters"] as Hashtable;
                    }

                    if (completers == null)
                    {
                        completers =
                            new Hashtable(CognifideSitecorePowerShellSnapIn.Completers.Count +
                                          MiscAutocompleteSets.Completers.Count);
                        options.Add("CustomArgumentCompleters", completers);
                    }

                    foreach (var miscCompleter in MiscAutocompleteSets.Completers)
                    {
                        AddCompleter(completers, miscCompleter);
                    }
                    foreach (var completer in CognifideSitecorePowerShellSnapIn.Completers)
                    {
                        AddCompleter(completers, completer);
                    }
                }
                return options;
            }
        }

        private static void AddCompleter(Hashtable completers, KeyValuePair<string, string> completer)
        {
            if (!completers.ContainsKey(completer.Key))
            {
                completers.Add(completer.Key,
                    ScriptBlock.Create(
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

            List<object> psResults;
            if (!session.TryInvokeInRunningSession($"Split-Path \"{lastToken}*\" -IsAbsolute", out psResults))
            {
                return result;
            }
            var splitPathResult = psResults.FirstOrDefault();
            var isAbsolute = splitPathResult != null && (bool) splitPathResult;

            if (isAbsolute)
            {
                if (session.TryInvokeInRunningSession($"Resolve-Path \"{lastToken}*\"", out psResults))
                {
                    var rpResult = psResults.Cast<PathInfo>().Select(p => p.Path);
                    WrapResults(truncatedCommand, rpResult, result, aceResponse);
                }
            }
            else
            {
                var commandLine = $"Resolve-Path \"{lastToken}*\" -Relative";
                if (session.TryInvokeInRunningSession(commandLine, out psResults))
                {
                    var rpResult = psResults.Cast<string>();
                    WrapResults(truncatedCommand, rpResult, result, aceResponse);
                }
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
                    ? $"{truncatedCommand}{(string.IsNullOrEmpty(truncatedCommand) ? string.Empty : " ")}'{l.Trim('\'')}'"
                    : truncatedCommand + l;
            }
                ));
        }

        public static void WrapResults3(string truncatedCommand, IEnumerable<string> tabExpandedResults,
            List<string> results, bool aceResponse)
        {
            if (truncatedCommand == null)
            {
                truncatedCommand = string.Empty;
            }
            var truncPosition = truncatedCommand.Length.ToString();
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
                            //content = content.Trim('\'', ' ', '&', '"');
                            content = content.Trim(' ', '&');
                            var leaf = content.Split('\\').Last().Trim('\'', '"');
                            return IsSitecoreItem(content)
                                ? $"Item|{leaf}|{truncPosition}|{content}"
                                : $"{type}|{leaf}|{truncPosition}|{content}";
                        case ("ParameterName"):
                            return $"Parameter|{content}";
                        default:
                            return l;
                    }
                }
                switch (type)
                {
                    case ("Variable"):
                    case ("ParameterName"):
                    case ("Command"):
                        return $"{truncatedCommand}{truncatedCommandTail}{content}";
                    case ("Property"):
                    case ("Method"):
                        return $"{truncatedCommand}{content}";
                    default:
                        return
                            $"{truncatedCommand}{truncatedCommandTail}{(content.StartsWith("& '") ? content.Substring(2) : content)}";
                }
            }).Select(p => aceResponse ? $"{p}|{truncPosition}" : p));
        }

        private static bool IsSitecoreItem(string path)
        {
            if (!string.IsNullOrEmpty(path) && path.IndexOf(':') > 0)
            {
                var colonIndex = path.IndexOf(':');
                if (colonIndex > 0)
                {
                    var db = path.Substring(0, colonIndex).ToLower();
                    return dbNames.Contains(db);
                }
            }
            return false;
        }

        private static void TruncatedCommand(ScriptSession session, string command, out string lastToken)
        {
            {
                switch (ScriptSession.PsVersion.Major)
                {
                    case (1):
                    case (2):
                        TruncatedCommand2(command, out lastToken);
                        break;
                    default:
                        TruncatedCommand3(session, command, out lastToken);
                        break;
                }
            }
        }

        private static string TruncatedCommand3(ScriptSession session, string command, out string lastToken)
        {
            const string TabExpansionHelper =
                @"function ScPsReplacementIndex( [string] $inputScript, [int]$cursorColumn ){ TabExpansion2 $inputScript $cursorColumn |% { $_.ReplacementIndex } }";

            if (session.TryInvokeInRunningSession(TabExpansionHelper))
            {

                var teCmd = new Command("ScPsReplacementIndex");
                teCmd.Parameters.Add("inputScript", command);
                teCmd.Parameters.Add("cursorColumn", command.Length);
                List<object> psResult;
                if (session.TryInvokeInRunningSession(teCmd, out psResult))
                {
                    var teResult = psResult.Cast<int>().FirstOrDefault();
                    lastToken = command.Substring(teResult);
                    return command.Substring(0, teResult);
                }
            }
            lastToken = string.Empty;
            return string.Empty;
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