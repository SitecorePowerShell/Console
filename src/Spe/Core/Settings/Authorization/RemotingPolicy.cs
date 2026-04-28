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
    ///
    /// <para>SECURITY MODEL (read before changing):</para>
    ///
    /// <para>The policy enforces two independent layers, both of which must hold for the
    /// design to be a real security boundary:</para>
    ///
    /// <para>1. <b>ConstrainedLanguage (engine layer).</b> When Full Language is unchecked,
    /// the runspace runs in CLM, which blocks type expressions
    /// (<c>[System.IO.File]::Delete</c>), <c>New-Object</c> for non-allowed types, direct
    /// .NET method invocation, COM, <c>Add-Type</c>, and reflection-style escapes.
    /// Without CLM, the AllowedCommands list is security theater - a script can bypass
    /// any cmdlet allowlist via <c>[Type]::Method()</c> etc. See
    /// <see cref="RemotingPolicyManager.NormalizeAllowedCommands"/>: under FullLanguage
    /// the allowlist is unconditionally ignored, by design.</para>
    ///
    /// <para>2. <b>CommandAst allowlist (parse-time scanner).</b>
    /// <see cref="ScriptValidator.ValidateScriptAgainstPolicy"/> walks the submitted
    /// script's AST <i>including nested script blocks</i> (function bodies, scriptblock
    /// arguments to <c>ForEach-Object</c> / <c>Invoke-Command</c>, default parameter
    /// expressions, hashtable initializers, etc.) and rejects any
    /// <see cref="System.Management.Automation.Language.CommandAst"/> whose name isn't on
    /// the allowlist (or the implicit <see cref="StreamBaseline"/>). Dynamic invocations
    /// (<c>&amp; $cmd</c>, <c>Invoke-Expression $string</c>) are rejected because the
    /// scanner can't verify what actually runs.</para>
    ///
    /// <para>The two layers cover each other: CLM blocks the .NET escape hatches the
    /// scanner can't see; the scanner blocks the cmdlet calls CLM doesn't directly
    /// police. Removing or weakening either layer collapses the guarantee.</para>
    ///
    /// <para>CMDLETS THAT NEUTRALIZE THE POLICY (do NOT add to a constrained
    /// AllowedCommands list):</para>
    /// <list type="bullet">
    ///   <item><c>Invoke-Expression</c> - evaluates an opaque string at runtime; bypasses
    ///   parse-time scanning entirely.</item>
    ///   <item><c>Import-Module</c> - loads cmdlets the operator never reviewed; can
    ///   bring in destructive surface that wasn't on the allowlist when the policy was
    ///   designed.</item>
    ///   <item><c>Set-Alias</c> / <c>New-Alias</c> - can rebind an allowlisted name
    ///   (e.g. <c>Get-Item</c>) to a non-allowlisted target (e.g. <c>Remove-Item</c>) at
    ///   runtime. The scanner sees only the parse-time name; the runtime alias resolution
    ///   substitutes the dangerous cmdlet.</item>
    ///   <item><c>Add-Type</c> - compiles and loads .NET code. CLM blocks the load at
    ///   runtime, but the cmdlet's presence on an allowlist signals a misconfigured
    ///   policy.</item>
    ///   <item><c>Update-TypeData</c> / <c>Update-FormatData</c> - extension surface that
    ///   can alter type behavior in ways the scanner doesn't model.</item>
    /// </list>
    /// </summary>
    public class RemotingPolicy
    {
        public string Name { get; }
        public bool FullLanguage { get; }
        public bool RestrictCommands { get; }
        public HashSet<string> AllowedCommands { get; }
        public HashSet<ID> ApprovedScriptIds { get; }
        public AuditLevel AuditLevel { get; }

        // Stream and output cmdlets are treated as implicit-allowed in every
        // policy. These are I/O primitives, not executable logic. Operators
        // shouldn't have to opt-in to "scripts may emit log output", and the
        // allowlist meaningfully polices behavior, not whether a message
        // reaches the verbose/debug/warning/error/info stream.
        // ConvertTo-Json is included as a shape-output primitive - it pairs
        // with Out-String for the policy-discovery contract ($RemotingPolicy
        // session variable surfaced to external consumers like the SPE MCP
        // server). Allowing it implicitly removes a sharp edge from the
        // discovery flow: probe scripts can serialize policy state without
        // requiring every CLM allowlist to remember "...and ConvertTo-Json".
        // Both short-name and module-qualified forms are covered so an
        // older client that prepends a fully-qualified bootstrap still
        // passes the scanner.
        private static readonly HashSet<string> StreamBaseline = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Write-Information",
            "Write-Debug",
            "Write-Verbose",
            "Write-Warning",
            "Write-Error",
            "Write-Output",
            "Write-Host",
            "Write-Progress",
            "Out-Default",
            "Out-Null",
            "Out-String",
            "ConvertTo-Json",
            "Microsoft.PowerShell.Utility\\Write-Information",
            "Microsoft.PowerShell.Utility\\Write-Debug",
            "Microsoft.PowerShell.Utility\\Write-Verbose",
            "Microsoft.PowerShell.Utility\\Write-Warning",
            "Microsoft.PowerShell.Utility\\Write-Error",
            "Microsoft.PowerShell.Utility\\Write-Output",
            "Microsoft.PowerShell.Utility\\Write-Host",
            "Microsoft.PowerShell.Utility\\Write-Progress",
            "Microsoft.PowerShell.Core\\Out-Default",
            "Microsoft.PowerShell.Core\\Out-Null",
            "Microsoft.PowerShell.Utility\\Out-String",
            "Microsoft.PowerShell.Utility\\ConvertTo-Json",
        };

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
        /// Under FullLanguage, all commands are allowed: the allowlist cannot
        /// meaningfully restrict callers because type expressions and direct
        /// .NET method invocation bypass CommandAst-based validation entirely
        /// (e.g. <c>[System.IO.File]::WriteAllText</c>,
        /// <c>[Activator]::CreateInstance</c>, <c>[ScriptBlock]::Create</c>).
        /// Under ConstrainedLanguage, commands are always restricted; an empty
        /// allowlist means no inline commands are permitted (except the
        /// implicit <see cref="StreamBaseline"/>).
        /// </summary>
        public bool IsCommandAllowed(string commandName)
        {
            if (!RestrictCommands) return true;
            if (string.IsNullOrEmpty(commandName)) return false;
            if (StreamBaseline.Contains(commandName)) return true;
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
