using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents a named restriction profile that combines language mode, command restrictions,
    /// module restrictions, audit level, and enforcement mode.
    /// Profiles are loaded flat from Spe.config (no inheritance) and customized via Sitecore config patching.
    /// </summary>
    public class RestrictionProfile
    {
        public string Name { get; }
        public PSLanguageMode LanguageMode { get; }
        public CommandRestrictionMode CommandMode { get; }
        public HashSet<string> Commands { get; }
        public ModuleRestrictions Modules { get; }
        public ItemPathRestrictions ItemPaths { get; }
        public AuditLevel AuditLevel { get; }
        public EnforcementMode Enforcement { get; }

        public RestrictionProfile(
            string name,
            PSLanguageMode languageMode,
            CommandRestrictionMode commandMode,
            HashSet<string> commands,
            ModuleRestrictions modules,
            AuditLevel auditLevel,
            EnforcementMode enforcement,
            ItemPathRestrictions itemPaths = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LanguageMode = languageMode;
            CommandMode = commandMode;
            Commands = commands ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Modules = modules;
            ItemPaths = itemPaths;
            AuditLevel = auditLevel;
            Enforcement = enforcement;
        }

        /// <summary>
        /// Returns true if the given command is allowed under this profile's restrictions.
        /// </summary>
        public bool IsCommandAllowed(string commandName)
        {
            if (CommandMode == CommandRestrictionMode.None) return true;
            if (string.IsNullOrEmpty(commandName)) return CommandMode != CommandRestrictionMode.Allowlist;

            if (CommandMode == CommandRestrictionMode.Allowlist)
            {
                return Commands.Contains(commandName);
            }

            // Blocklist mode
            return !Commands.Contains(commandName);
        }

        /// <summary>
        /// Returns true if the given module is allowed under this profile's restrictions.
        /// </summary>
        public bool IsModuleAllowed(string moduleName)
        {
            if (Modules == null || !Modules.RestrictModules) return true;
            if (string.IsNullOrEmpty(moduleName)) return false;

            return Modules.AllowedModules.Contains(moduleName);
        }

        /// <summary>
        /// Returns true if the given Sitecore item path is allowed under this profile's restrictions.
        /// Uses prefix matching: denying /foo also denies /foo/bar/baz.
        /// </summary>
        public bool IsItemPathAllowed(string itemPath)
        {
            if (ItemPaths == null || ItemPaths.Mode == CommandRestrictionMode.None) return true;
            if (string.IsNullOrEmpty(itemPath)) return ItemPaths.Mode != CommandRestrictionMode.Allowlist;

            if (ItemPaths.Mode == CommandRestrictionMode.Allowlist)
            {
                foreach (var allowed in ItemPaths.Paths)
                {
                    if (itemPath.StartsWith(allowed, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                return false;
            }

            // Blocklist mode
            foreach (var blocked in ItemPaths.Paths)
            {
                if (itemPath.StartsWith(blocked, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// The unrestricted profile used as default when no profile is configured.
        /// </summary>
        public static readonly RestrictionProfile Unrestricted = new RestrictionProfile(
            "unrestricted",
            PSLanguageMode.FullLanguage,
            CommandRestrictionMode.None,
            null,
            null,
            AuditLevel.None,
            EnforcementMode.Enforce);

        /// <summary>
        /// A deny-all profile used when a referenced profile name is invalid.
        /// Uses an empty allowlist so all commands are rejected.
        /// </summary>
        public static readonly RestrictionProfile DenyAll = new RestrictionProfile(
            "deny-all",
            PSLanguageMode.RestrictedLanguage,
            CommandRestrictionMode.Allowlist,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            null,
            AuditLevel.Violations,
            EnforcementMode.Enforce,
            new ItemPathRestrictions(CommandRestrictionMode.Allowlist, new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
    }

    public enum CommandRestrictionMode
    {
        None,
        Blocklist,
        Allowlist
    }

    public enum AuditLevel
    {
        None,
        Violations,
        Standard,
        Full
    }

    public enum EnforcementMode
    {
        Enforce,
        Audit
    }

    public class ModuleRestrictions
    {
        public bool RestrictModules { get; }
        public string AutoloadPreference { get; }
        public HashSet<string> AllowedModules { get; }

        public ModuleRestrictions(string autoloadPreference, HashSet<string> allowedModules)
        {
            AutoloadPreference = autoloadPreference ?? "None";
            AllowedModules = allowedModules ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RestrictModules = true;
        }
    }

    public class ItemPathRestrictions
    {
        public CommandRestrictionMode Mode { get; }
        public HashSet<string> Paths { get; }

        public ItemPathRestrictions(CommandRestrictionMode mode, HashSet<string> paths)
        {
            Mode = mode;
            Paths = paths ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

}
