using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Xml;
using Sitecore.Configuration;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Loads and manages restriction profiles from Spe.config.
    /// Profiles are flat (no inheritance) and customized via standard Sitecore config patching.
    /// </summary>
    public static class RestrictionProfileManager
    {
        private static readonly Dictionary<string, RestrictionProfile> _profiles =
            new Dictionary<string, RestrictionProfile>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> _serviceProfileMap =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private static bool _initialized;
        private static readonly object _lock = new object();

        /// <summary>
        /// Gets a profile by name. Returns null if not found.
        /// </summary>
        public static RestrictionProfile GetProfile(string profileName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(profileName)) return null;

            if (!_profiles.TryGetValue(profileName, out var profile)) return null;

            return ProfileOverrideProvider.GetMergedProfile(profile);
        }

        /// <summary>
        /// Resolves the effective profile for a service + JWT scope combination.
        /// If the scope maps to a known profile, returns it. Otherwise returns the service's default profile.
        /// If the service has no profile configured, returns the Unrestricted profile.
        /// </summary>
        public static RestrictionProfile ResolveProfile(string serviceName, string scope)
        {
            EnsureInitialized();

            // If scope maps to a known profile name, it takes precedence.
            // The scope is the caller's explicitly declared restriction level via JWT claim.
            if (!string.IsNullOrEmpty(scope))
            {
                if (_profiles.TryGetValue(scope, out var scopeProfile))
                {
                    return ProfileOverrideProvider.GetMergedProfile(scopeProfile);
                }
            }

            return GetServiceProfile(serviceName) ?? RestrictionProfile.Unrestricted;
        }

        /// <summary>
        /// Gets the profile configured for a specific service.
        /// </summary>
        public static RestrictionProfile GetServiceProfile(string serviceName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(serviceName)) return null;

            if (_serviceProfileMap.TryGetValue(serviceName, out var profileName))
            {
                if (_profiles.TryGetValue(profileName, out var profile))
                {
                    return ProfileOverrideProvider.GetMergedProfile(profile);
                }
                PowerShellLog.Warn($"Service '{serviceName}' references unknown profile '{profileName}'. Falling back to unrestricted.");
            }

            return null;
        }

        private static void EnsureInitialized()
        {
            if (_initialized) return;
            lock (_lock)
            {
                if (_initialized) return;
                LoadProfiles();
                LoadServiceMappings();
                _initialized = true;
            }
        }

        private static void LoadProfiles()
        {
            var configNode = Factory.GetConfigNode("powershell/restrictionProfiles");
            if (configNode == null) return;

            foreach (XmlNode child in configNode.ChildNodes)
            {
                var element = child as XmlElement;
                if (element == null || element.Name != "profile") continue;

                var name = element.GetAttribute("name");
                if (string.IsNullOrEmpty(name)) continue;

                try
                {
                    var profile = ParseProfile(element);
                    _profiles[name] = profile;
                    PowerShellLog.Info($"Loaded restriction profile: {name} (LanguageMode={profile.LanguageMode}, CommandMode={profile.CommandMode}, Enforcement={profile.Enforcement})");
                }
                catch (Exception ex)
                {
                    PowerShellLog.Error($"Failed to load restriction profile '{name}'.", ex);
                }
            }
        }

        private static RestrictionProfile ParseProfile(XmlElement element)
        {
            var name = element.GetAttribute("name");

            // Language mode
            var langMode = PSLanguageMode.FullLanguage;
            var langModeAttr = element.GetAttribute("languageMode");
            if (!string.IsNullOrEmpty(langModeAttr))
            {
                Enum.TryParse(langModeAttr, true, out langMode);
            }

            // Audit level
            var auditLevel = AuditLevel.None;
            var auditAttr = element.GetAttribute("auditLevel");
            if (!string.IsNullOrEmpty(auditAttr))
            {
                Enum.TryParse(auditAttr, true, out auditLevel);
            }

            // Enforcement mode
            var enforcement = EnforcementMode.Enforce;
            var enforcementAttr = element.GetAttribute("enforcement");
            if (!string.IsNullOrEmpty(enforcementAttr))
            {
                Enum.TryParse(enforcementAttr, true, out enforcement);
            }

            // Command restrictions
            var commandMode = CommandRestrictionMode.None;
            var commands = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var restrictionsNode = element.SelectSingleNode("commandRestrictions") as XmlElement;
            if (restrictionsNode != null)
            {
                var mode = restrictionsNode.GetAttribute("mode");
                if ("allowlist".Equals(mode, StringComparison.OrdinalIgnoreCase))
                {
                    commandMode = CommandRestrictionMode.Allowlist;
                    ParseCommandList(restrictionsNode, "allowedCommands/command", commands);
                }
                else
                {
                    commandMode = CommandRestrictionMode.Blocklist;
                    ParseCommandList(restrictionsNode, "blockedCommands/command", commands);
                }
            }

            // Module restrictions
            ModuleRestrictions modules = null;
            var modulesNode = element.SelectSingleNode("modules") as XmlElement;
            if (modulesNode != null)
            {
                var autoload = modulesNode.GetAttribute("autoload") ?? "None";
                var allowedModules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var moduleNodes = modulesNode.SelectNodes("module");
                if (moduleNodes != null)
                {
                    foreach (XmlNode moduleNode in moduleNodes)
                    {
                        var mod = moduleNode.InnerText?.Trim();
                        if (!string.IsNullOrEmpty(mod))
                        {
                            allowedModules.Add(mod);
                        }
                    }
                }
                modules = new ModuleRestrictions(autoload, allowedModules);
            }

            // Standard field restrictions
            StandardFieldRestrictions standardFields = null;
            var sfNode = element.SelectSingleNode("standardFieldRestrictions") as XmlElement;
            if (sfNode != null)
            {
                var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var blocked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (XmlNode tierNode in sfNode.SelectNodes("tier"))
                {
                    var tierElement = tierNode as XmlElement;
                    if (tierElement == null) continue;

                    var tierDefault = tierElement.GetAttribute("default");
                    var isAllowed = "allow".Equals(tierDefault, StringComparison.OrdinalIgnoreCase);

                    foreach (XmlNode fieldNode in tierElement.SelectNodes("field"))
                    {
                        var fieldName = fieldNode.InnerText?.Trim();
                        if (string.IsNullOrEmpty(fieldName)) continue;

                        if (isAllowed)
                            allowed.Add(fieldName);
                        else
                            blocked.Add(fieldName);
                    }
                }

                standardFields = new StandardFieldRestrictions(allowed, blocked);
            }

            return new RestrictionProfile(name, langMode, commandMode, commands, modules, standardFields, auditLevel, enforcement);
        }

        private static void ParseCommandList(XmlElement parent, string xpath, HashSet<string> commands)
        {
            var nodes = parent.SelectNodes(xpath);
            if (nodes == null) return;

            foreach (XmlNode node in nodes)
            {
                var cmd = node.InnerText?.Trim();
                if (!string.IsNullOrEmpty(cmd))
                {
                    commands.Add(cmd);
                }
            }
        }

        private static void LoadServiceMappings()
        {
            var servicesNode = Factory.GetConfigNode("powershell/services");
            if (servicesNode == null) return;

            foreach (XmlNode child in servicesNode.ChildNodes)
            {
                var element = child as XmlElement;
                if (element == null) continue;

                var profile = element.GetAttribute("profile");
                if (!string.IsNullOrEmpty(profile))
                {
                    _serviceProfileMap[element.Name] = profile;
                }
            }
        }
    }
}
