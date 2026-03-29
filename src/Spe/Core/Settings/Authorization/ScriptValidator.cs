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

        /// <summary>
        /// Validates a script against a restriction profile's command restrictions.
        /// Supports audit-only enforcement mode -- logs violations but returns true when enforcement=audit.
        /// </summary>
        private static bool ValidateScriptAgainstProfileInternal(RestrictionProfile profile, string script, string userName, string serviceName, out string blockedCommand)
        {
            blockedCommand = null;
            if (profile == null || string.IsNullOrEmpty(script)) return true;
            if (profile.CommandMode == CommandRestrictionMode.None) return true;

            var ast = Parser.ParseInput(script, out _, out _);
            var commandAsts = ast.FindAll(node => node is CommandAst, true).Cast<CommandAst>().ToList();

            foreach (var commandAst in commandAsts)
            {
                var commandName = commandAst.GetCommandName();

                if (!profile.IsCommandAllowed(commandName ?? string.Empty))
                {
                    blockedCommand = commandName ?? "(dynamic invocation)";

                    if (profile.AuditLevel >= AuditLevel.Violations)
                    {
                        if (profile.Enforcement == EnforcementMode.Audit)
                        {
                            PowerShellLog.Audit(
                                "SPE.Security [AUDIT] User={0} Service={1} Profile={2} WouldBlock={3} (enforcement=audit, execution allowed)",
                                userName ?? "unknown", serviceName ?? "unknown", profile.Name, blockedCommand);
                        }
                        else
                        {
                            PowerShellLog.Audit(
                                "SPE.Security [VIOLATION] User={0} Service={1} Profile={2} BlockedCommand={3}",
                                userName ?? "unknown", serviceName ?? "unknown", profile.Name, blockedCommand);
                        }
                    }

                    if (profile.Enforcement == EnforcementMode.Audit)
                    {
                        blockedCommand = null; // Clear -- not actually blocking
                        continue; // Check remaining commands for audit logging
                    }

                    return false;
                }
            }

            return true;
        }

        // Static facades

        public static bool ValidateScript(string serviceName, string script, string scope, out string blockedCommand)
        {
            return Instance.ValidateScriptInternal(serviceName, script, scope, out blockedCommand);
        }

        /// <summary>
        /// Validates a script against a restriction profile.
        /// Returns true if the script is allowed to execute.
        /// In audit mode, always returns true but logs violations.
        /// </summary>
        public static bool ValidateScriptAgainstProfile(RestrictionProfile profile, string script, string userName, string serviceName, out string blockedCommand)
        {
            return ValidateScriptAgainstProfileInternal(profile, script, userName, serviceName, out blockedCommand);
        }
    }
}
