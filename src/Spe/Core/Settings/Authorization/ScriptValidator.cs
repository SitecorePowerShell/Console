using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Xml;
using Sitecore.Configuration;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    public class ScriptValidator
    {
        private readonly Dictionary<string, ScopeRestriction> _scopeRestrictions = new Dictionary<string, ScopeRestriction>(StringComparer.OrdinalIgnoreCase);

        private static ScriptValidator _instance;

        private static ScriptValidator Instance
        {
            get
            {
                if (_instance != null) return _instance;

                var instance = new ScriptValidator();
                var configNode = Factory.GetConfigNode("powershell/scopeRestrictions");
                if (configNode != null)
                {
                    foreach (XmlNode child in configNode.ChildNodes)
                    {
                        var element = child as XmlElement;
                        if (element == null) continue;

                        var scopeName = element.GetAttribute("name");
                        if (string.IsNullOrEmpty(scopeName)) continue;

                        var mode = element.GetAttribute("mode");
                        if (string.IsNullOrEmpty(mode)) mode = "blocklist";

                        var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        foreach (XmlNode commandNode in element.SelectNodes("command"))
                        {
                            var cmd = commandNode.InnerText?.Trim();
                            if (!string.IsNullOrEmpty(cmd))
                            {
                                commands.Add(cmd);
                            }
                        }

                        if (commands.Count > 0)
                        {
                            instance._scopeRestrictions[scopeName] = new ScopeRestriction { Commands = commands, Mode = mode };
                        }
                    }
                }

                _instance = instance;
                return _instance;
            }
        }

        private class ScopeRestriction
        {
            public HashSet<string> Commands { get; set; }
            public string Mode { get; set; }
        }

        private bool ValidateScriptInternal(string serviceName, string script, string scope, out string blockedCommand)
        {
            blockedCommand = null;
            if (string.IsNullOrEmpty(script)) return true;

            var serviceRestrictions = WebServiceSettings.GetCommandRestrictions(serviceName);

            ScopeRestriction scopeRestriction = null;
            var hasScopeRestrictions = false;
            if (!string.IsNullOrEmpty(scope))
            {
                if (_scopeRestrictions.TryGetValue(scope, out scopeRestriction))
                {
                    hasScopeRestrictions = true;
                }
                else if (_scopeRestrictions.Count > 0)
                {
                    // Scope restrictions are configured but the requested scope is unknown.
                    // Log a warning -- this could indicate a typo or misconfiguration.
                    PowerShellLog.Warn($"ScriptValidator: unknown scope '{scope}' requested; no matching scopeRestriction configured. Script will not have scope-level restrictions applied.");
                }
            }

            // No restrictions configured at all -- skip parsing
            if (serviceRestrictions == null && !hasScopeRestrictions) return true;

            var ast = Parser.ParseInput(script, out _, out _);
            var commandAsts = ast.FindAll(node => node is CommandAst, true).Cast<CommandAst>().ToList();

            // Check service-level restrictions (read from WebServiceSettings)
            if (serviceRestrictions != null)
            {
                var isAllowlist = serviceRestrictions.Mode.Equals("allowlist", StringComparison.OrdinalIgnoreCase);

                foreach (var commandAst in commandAsts)
                {
                    var commandName = commandAst.GetCommandName();

                    if (isAllowlist)
                    {
                        if (commandName == null || !serviceRestrictions.Commands.Contains(commandName))
                        {
                            blockedCommand = commandName ?? "(dynamic invocation)";
                            return false;
                        }
                    }
                    else
                    {
                        if (commandName != null && serviceRestrictions.Commands.Contains(commandName))
                        {
                            blockedCommand = commandName;
                            return false;
                        }
                    }
                }
            }

            // Check scope-level restrictions (supports both blocklist and allowlist modes)
            if (hasScopeRestrictions)
            {
                var isScopeAllowlist = scopeRestriction.Mode.Equals("allowlist", StringComparison.OrdinalIgnoreCase);

                foreach (var commandAst in commandAsts)
                {
                    var commandName = commandAst.GetCommandName();

                    if (isScopeAllowlist)
                    {
                        if (commandName == null || !scopeRestriction.Commands.Contains(commandName))
                        {
                            blockedCommand = commandName ?? "(dynamic invocation)";
                            return false;
                        }
                    }
                    else
                    {
                        if (commandName != null && scopeRestriction.Commands.Contains(commandName))
                        {
                            blockedCommand = commandName;
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        // Static facade
        public static bool ValidateScript(string serviceName, string script, string scope, out string blockedCommand)
        {
            return Instance.ValidateScriptInternal(serviceName, script, scope, out blockedCommand);
        }
    }
}
