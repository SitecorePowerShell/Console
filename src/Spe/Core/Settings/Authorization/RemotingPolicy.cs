using System;
using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents a named remoting policy that controls language mode and command access
    /// for remote script execution sessions. Policies are defined as Sitecore content items
    /// under the Security/Policies node, resolved by item ID.
    /// </summary>
    public class RemotingPolicy
    {
        public string Name { get; }
        public bool FullLanguage { get; }
        public bool RestrictCommands { get; }
        public HashSet<string> AllowedCommands { get; }
        public HashSet<ID> ApprovedScriptIds { get; }
        public AuditLevel AuditLevel { get; }

        public RemotingPolicy(
            string name,
            bool fullLanguage,
            bool restrictCommands,
            HashSet<string> allowedCommands,
            HashSet<ID> approvedScriptIds,
            AuditLevel auditLevel)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            FullLanguage = fullLanguage;
            RestrictCommands = restrictCommands;
            AllowedCommands = allowedCommands ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ApprovedScriptIds = approvedScriptIds ?? new HashSet<ID>();
            AuditLevel = auditLevel;
        }

        /// <summary>
        /// The effective PowerShell language mode for this policy.
        /// </summary>
        public PSLanguageMode LanguageMode => FullLanguage
            ? PSLanguageMode.FullLanguage
            : PSLanguageMode.ConstrainedLanguage;

        /// <summary>
        /// Returns true if the given command is allowed under this policy.
        /// Under ConstrainedLanguage (Full Language unchecked), commands are always restricted.
        /// An empty allowlist means no inline commands are permitted.
        /// Under FullLanguage, all commands are allowed unless an explicit allowlist is provided.
        /// </summary>
        public bool IsCommandAllowed(string commandName)
        {
            if (!RestrictCommands) return true;
            if (string.IsNullOrEmpty(commandName)) return false;
            return AllowedCommands.Contains(commandName);
        }

        /// <summary>
        /// Returns true if the given script item is approved for elevated execution
        /// under this policy.
        /// </summary>
        public bool IsScriptApproved(ID scriptItemId)
        {
            if (scriptItemId == (ID)null) return false;
            return ApprovedScriptIds.Count > 0 && ApprovedScriptIds.Contains(scriptItemId);
        }

        /// <summary>
        /// The unrestricted policy used as default when no policy is configured.
        /// </summary>
        public static readonly RemotingPolicy Unrestricted = new RemotingPolicy(
            "unrestricted",
            fullLanguage: true,
            restrictCommands: false,
            allowedCommands: null,
            approvedScriptIds: null,
            auditLevel: AuditLevel.Standard);

        /// <summary>
        /// A deny-all policy used when a referenced policy name is invalid.
        /// Uses an empty allowlist so all commands are rejected.
        /// </summary>
        public static readonly RemotingPolicy DenyAll = new RemotingPolicy(
            "deny-all",
            fullLanguage: false,
            restrictCommands: true,
            allowedCommands: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            approvedScriptIds: null,
            auditLevel: AuditLevel.Violations);
    }

    public enum AuditLevel
    {
        None,
        Violations,
        Standard,
        Full
    }
}
