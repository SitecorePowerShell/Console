using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Represents a named restriction profile that combines language mode, command restrictions,
    /// module restrictions, standard field restrictions, audit level, and enforcement mode.
    /// Profiles are loaded flat from Spe.config (no inheritance) and customized via Sitecore config patching.
    /// </summary>
    public class RestrictionProfile
    {
        public string Name { get; }
        public PSLanguageMode LanguageMode { get; }
        public CommandRestrictionMode CommandMode { get; }
        public HashSet<string> Commands { get; }
        public ModuleRestrictions Modules { get; }
        public StandardFieldRestrictions StandardFields { get; }
        public AuditLevel AuditLevel { get; }
        public EnforcementMode Enforcement { get; }

        public RestrictionProfile(
            string name,
            PSLanguageMode languageMode,
            CommandRestrictionMode commandMode,
            HashSet<string> commands,
            ModuleRestrictions modules,
            StandardFieldRestrictions standardFields,
            AuditLevel auditLevel,
            EnforcementMode enforcement)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            LanguageMode = languageMode;
            CommandMode = commandMode;
            Commands = commands ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Modules = modules;
            StandardFields = standardFields;
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
        /// Returns true if the given standard field name is allowed for writing under this profile.
        /// Only relevant for the content-editor profile.
        /// </summary>
        public bool IsStandardFieldWriteAllowed(string fieldName)
        {
            if (StandardFields == null) return true;
            if (string.IsNullOrEmpty(fieldName)) return true;
            if (!fieldName.StartsWith("__")) return true; // Not a standard field

            return StandardFields.IsFieldAllowed(fieldName);
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
            null,
            AuditLevel.None,
            EnforcementMode.Enforce);
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

    public class StandardFieldRestrictions
    {
        private readonly HashSet<string> _allowedFields;
        private readonly HashSet<string> _blockedFields;

        public StandardFieldRestrictions(HashSet<string> allowedFields, HashSet<string> blockedFields)
        {
            _allowedFields = allowedFields ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _blockedFields = blockedFields ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if the standard field is allowed for writing.
        /// Blocked fields take precedence over allowed fields.
        /// If neither list contains the field, it is blocked by default (standard fields are opt-in).
        /// </summary>
        public bool IsFieldAllowed(string fieldName)
        {
            if (_blockedFields.Contains(fieldName)) return false;
            if (_allowedFields.Contains(fieldName)) return true;
            return false; // Standard fields blocked by default
        }
    }
}
