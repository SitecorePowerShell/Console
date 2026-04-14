using System.Linq;
using System.Management.Automation.Language;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    public class ScriptValidator
    {
        /// <summary>
        /// Validates a script against a remoting policy's command restrictions.
        /// Returns false if the script contains a command not in the policy's allowlist.
        /// </summary>
        public static bool ValidateScriptAgainstPolicy(RemotingPolicy policy, string script, string userName, string serviceName, out string blockedCommand)
        {
            blockedCommand = null;
            if (policy == null || string.IsNullOrEmpty(script)) return true;
            if (!policy.RestrictCommands) return true;

            var ast = Parser.ParseInput(script, out _, out _);
            var commandAsts = ast.FindAll(node => node is CommandAst, true).Cast<CommandAst>().ToList();

            foreach (var commandAst in commandAsts)
            {
                var commandName = commandAst.GetCommandName();

                // Reject dynamic invocations (null command name) since the actual
                // command cannot be verified against the policy's allowlist.
                var isAllowed = commandName != null && policy.IsCommandAllowed(commandName);
                if (!isAllowed)
                {
                    blockedCommand = commandName ?? "(dynamic invocation)";

                    if (policy.AuditLevel >= AuditLevel.Violations)
                    {
                        PowerShellLog.Audit(
                            "[Policy] action=commandBlocked service={0} policy={1} command={2}",
                            serviceName ?? "unknown", policy.Name, blockedCommand);
                    }

                    return false;
                }
            }

            return true;
        }
    }
}
