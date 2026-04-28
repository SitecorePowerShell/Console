using System.Linq;
using System.Management.Automation.Language;

namespace Spe.Core.Settings.Authorization
{
    public class ScriptValidator
    {
        /// <summary>
        /// Validates a script against a remoting policy's command restrictions.
        /// Returns false if the script contains a command not in the policy's allowlist.
        ///
        /// Walks the entire AST including nested script blocks (function bodies,
        /// scriptblock arguments, default parameter expressions, hashtable
        /// initializers) so a cmdlet call hidden inside a function definition
        /// gets scanned the same way as a top-level call. Dynamic invocations
        /// (`&amp; $variable`, name-as-expression) are rejected because the
        /// scanner can't verify what runs.
        ///
        /// This is one of two enforcement layers - see
        /// <see cref="RemotingPolicy"/> for the full security model and the list
        /// of cmdlets that neutralize the policy if added to an allowlist.
        ///
        /// The caller is responsible for auditing rejections - this method no
        /// longer emits its own audit log because the remoting handler already
        /// emits [Remoting] action=scriptRejectedByPolicy with richer context
        /// (user, ip, clientSession, rid) on the same rejection.
        /// </summary>
        public static bool ValidateScriptAgainstPolicy(RemotingPolicy policy, string script, out string blockedCommand)
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
                    return false;
                }
            }

            return true;
        }
    }
}
