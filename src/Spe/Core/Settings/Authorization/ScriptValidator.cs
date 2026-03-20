using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;
using System.Xml;
using Sitecore.Configuration;

namespace Spe.Core.Settings.Authorization
{
    public class ScriptValidator
    {
        private readonly Dictionary<string, HashSet<string>> _scopeRestrictions = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private static ScriptValidator _instance;

        private static ScriptValidator Instance =>
            _instance ?? (_instance = (ScriptValidator)Factory.CreateObject("powershell/scopeRestrictions", true));

        // Called by Sitecore config factory via hint="raw:AddScopeRestriction"
        public void AddScopeRestriction(XmlNode node)
        {
            var element = node as XmlElement;
            if (element == null) return;

            var scopeName = element.GetAttribute("name");
            if (string.IsNullOrEmpty(scopeName)) return;

            var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (XmlNode commandNode in element.SelectNodes("command"))
            {
                var cmd = commandNode.InnerText?.Trim();
                if (!string.IsNullOrEmpty(cmd))
                {
                    commands.Add(cmd);
                }
            }

            _scopeRestrictions[scopeName] = commands;
        }

        private bool ValidateScriptInternal(string serviceName, string script, string scope, out string blockedCommand)
        {
            blockedCommand = null;
            if (string.IsNullOrEmpty(script)) return true;

            var serviceRestrictions = WebServiceSettings.GetCommandRestrictions(serviceName);
            HashSet<string> scopeBlocked = null;
            var hasScopeRestrictions = !string.IsNullOrEmpty(scope) && _scopeRestrictions.TryGetValue(scope, out scopeBlocked);

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

            // Check scope-level restrictions
            if (hasScopeRestrictions)
            {
                foreach (var commandAst in commandAsts)
                {
                    var commandName = commandAst.GetCommandName();
                    if (commandName != null && scopeBlocked.Contains(commandName))
                    {
                        blockedCommand = commandName;
                        return false;
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
