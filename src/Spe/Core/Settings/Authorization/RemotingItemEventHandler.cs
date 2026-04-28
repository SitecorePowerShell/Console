using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.SecurityModel;
using Spe.Core.Diagnostics;

namespace Spe.Core.Settings.Authorization
{
    /// <summary>
    /// Event handler for remoting security items (Shared Secret Client, OAuth
    /// Client, Remoting Policy). Invalidates caches on save and validates
    /// template-specific save-time constraints.
    /// </summary>
    public class RemotingItemEventHandler
    {
        // Client_id values that are almost always operator typos or placeholders.
        // Rejecting them at save time prevents accidental catch-all authentication.
        private static readonly HashSet<string> ReservedClientIds =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "admin", "administrator", "root", "test", "testing",
                "user", "client", "null", "undefined", "default", "example"
            };

        private const int MinClientIdLength = 8;

        // Cmdlets that neutralize a Constrained Language allowlist if added to it.
        // Their presence is logged at Warn level on save, but not rejected -
        // the operator may have a niche legitimate use, and the YAML deserializer
        // bypasses this handler under EventDisabler anyway. Logging gives the
        // audit trail needed when a misconfigured policy later causes an incident.
        // See class-level security model on RemotingPolicy for the rationale per entry.
        private static readonly Dictionary<string, string> DangerousAllowlistedCommands =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Invoke-Expression", "dynamicEval" },
                { "Invoke-Command",    "runtimeFileLoadOrRemoteEscape" },
                { "Import-Module",     "loadsUnreviewedCmdlets" },
                { "Start-Process",     "processEscape" },
                { "Set-Alias",         "aliasRebindBypass" },
                { "New-Alias",         "aliasRebindBypass" },
                { "Add-Type",          "dotNetCompileLoad" },
                { "Update-TypeData",   "typeExtension" },
                { "Update-FormatData", "formatExtension" },
            };

        /// <summary>
        /// Returns the reason code if the given (canonical short-form) cmdlet
        /// name is on the dangerous-allowlist list, otherwise null.
        /// Public + static so unit tests can verify the registry without
        /// invoking the full save pipeline.
        /// </summary>
        public static string IsDangerousAllowlisted(string commandName)
        {
            if (string.IsNullOrEmpty(commandName)) return null;
            return DangerousAllowlistedCommands.TryGetValue(commandName, out var reason) ? reason : null;
        }

        // Cmdlet-name shape: optional dotted module qualifier + Verb-Noun.
        // Each module segment and the verb/noun must start with a letter and
        // contain only word chars (letter/digit/underscore). Anchored end-to-end
        // so partial matches (e.g. "Get-Item; Remove-Item") are rejected.
        private static readonly Regex CmdletNameShape = new Regex(
            @"^([A-Za-z][\w]*(\.[A-Za-z][\w]*)*\\)?[A-Za-z][\w]*-[A-Za-z][\w]*$",
            RegexOptions.Compiled);

        /// <summary>
        /// Invalidates security caches when remoting items are saved.
        /// </summary>
        internal void OnItemSaved(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var item = Event.ExtractParameter<Item>(args, 0);
            if (item == null) return;

            if (item.TemplateID == Templates.RemotingPolicy.Id)
            {
                RemotingPolicyManager.Invalidate();
            }
            else if (item.TemplateID == Templates.SharedSecretClient.Id ||
                     item.TemplateID == Templates.OAuthClient.Id)
            {
                HttpRuntime.Cache.Remove("Spe.RemotingClients");
                HttpRuntime.Cache.Remove("Spe.RemotingClients.ByAccessKeyId");
                HttpRuntime.Cache.Remove("Spe.RemotingClients.ByIssuerClientId");
                HttpRuntime.Cache.Remove("Spe.RemotingClients.KidStates");
            }
        }

        /// <summary>
        /// Validates Remoting Client constraints before save.
        /// Shared Secret Client: unique Access Key Id across all clients.
        /// OAuth Client: no wildcard / short / reserved client_ids; unique
        /// (Allowed Issuer, client_id) pairs across all clients.
        /// </summary>
        internal void OnItemSaving(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            if (!(args is SitecoreEventArgs scArgs) || scArgs.Parameters.Length < 1) return;

            var item = scArgs.Parameters[0] as Item;
            if (item == null) return;

            if (item.TemplateID == Templates.SharedSecretClient.Id)
            {
                ValidateSharedSecretClient(item, scArgs);
            }
            else if (item.TemplateID == Templates.OAuthClient.Id)
            {
                ValidateOAuthClient(item, scArgs);
            }
            else if (item.TemplateID == Templates.RemotingPolicy.Id)
            {
                SanitizeRemotingPolicy(item);
            }
        }

        /// <summary>
        /// Pure-function helper that cleans the multi-line Allowed Commands text
        /// before storage. Lenient sanitize semantics (operator decision):
        ///   - trim each line
        ///   - drop blank lines
        ///   - drop comment lines (leading '#'); they are otherwise saved as
        ///     no-op allowlist entries that match nothing
        ///   - drop lines that don't match cmdlet-name shape (Verb-Noun, with
        ///     optional dotted module qualifier)
        ///   - strip module qualifier to canonical short form
        ///     (<c>Microsoft.PowerShell.Utility\ConvertTo-Json</c> -> <c>ConvertTo-Json</c>)
        ///     so allowlist storage matches how most scripts call cmdlets and
        ///     <c>Get-Item</c> + <c>Module\Get-Item</c> dedup to one entry
        ///   - dedup case-insensitive, preserving first occurrence
        ///   - return CRLF-joined result for stable Sitecore storage
        ///
        /// Qualifier stripping eliminates the false-negative class where an
        /// allowlist entry doesn't match a script's qualified call (or vice
        /// versa). The qualified-form-as-shadowing-defense argument doesn't
        /// hold under either language mode (CLM blocks function-name shadowing
        /// of cmdlets at the engine level; FL allows enough .NET surface that
        /// the allowlist isn't the security boundary anyway), so collapsing to
        /// short form costs nothing.
        ///
        /// Bad input is silently dropped rather than rejecting the save - the
        /// audit log captures what got removed (see <see cref="SanitizeRemotingPolicy"/>),
        /// and operators see a normalized field value next time they open the
        /// item. This trades user-feedback for forgiveness of typos and stale
        /// hand-edits.
        ///
        /// Public + static so the unit test harness can exercise the matrix
        /// without mocking a Sitecore <see cref="Item"/>. Mirrors the
        /// pure-helper pattern from #1485 (IsValidTokenType, IsValidAzp).
        /// </summary>
        public static string SanitizeAllowedCommands(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cleaned = new List<string>();

            foreach (var rawLine in input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                if (!CmdletNameShape.IsMatch(line)) continue;

                // Strip module qualifier to canonical short form.
                var backslash = line.LastIndexOf('\\');
                if (backslash >= 0) line = line.Substring(backslash + 1);

                if (!seen.Add(line)) continue;
                cleaned.Add(line);
            }

            return string.Join("\r\n", cleaned);
        }

        private static void SanitizeRemotingPolicy(Item item)
        {
            var field = item.Fields[Templates.RemotingPolicy.Fields.AllowedCommands];
            if (field == null) return;

            var raw = field.Value;
            var cleaned = SanitizeAllowedCommands(raw);

            string[] cleanedLines = cleaned.Length == 0
                ? Array.Empty<string>()
                : cleaned.Split(new[] { '\r', '\n' }, StringSplitOptions.None);

            if (!string.Equals(cleaned, raw, StringComparison.Ordinal))
            {
                // Audit what got dropped so operators can trace silent removals.
                var rawLineCount = (raw ?? string.Empty)
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Length;
                var dropped = rawLineCount - cleanedLines.Length;

                field.Value = cleaned;

                if (dropped > 0)
                {
                    PowerShellLog.Info(
                        $"[RemotingPolicy] action=allowedCommandsSanitized policy={item.Name} droppedLines={dropped} keptLines={cleanedLines.Length}");
                }
            }

            // Warn-only: dangerous cmdlets neutralize the policy if present.
            // Operator can still save - they may have a niche legitimate use,
            // and the YAML path bypasses this handler under EventDisabler. The
            // audit trail is what makes this useful when a misconfigured policy
            // later causes an incident.
            foreach (var cmd in cleanedLines)
            {
                var reason = IsDangerousAllowlisted(cmd);
                if (reason != null)
                {
                    PowerShellLog.Warn(
                        $"[RemotingPolicy] action=dangerousCommandOnAllowlist policy={item.Name} command={cmd} reason={reason}");
                }
            }
        }

        private static void ValidateSharedSecretClient(Item item, SitecoreEventArgs scArgs)
        {
            var accessKeyId = item.Fields[Templates.SharedSecretClient.Fields.AccessKeyId]?.Value?.Trim();
            if (string.IsNullOrEmpty(accessKeyId)) return;

            if (HasDuplicateAccessKeyId(item, accessKeyId))
            {
                PowerShellLog.Error(
                    $"[RemotingClient] action=saveDenied reason=duplicateAccessKeyId remotingClient={item.Name} accessKeyId={LogSanitizer.SanitizeValue(accessKeyId)}");
                scArgs.Result.Cancel = true;
                scArgs.Result.Messages.Add(
                    $"A Shared Secret Client with Access Key Id '{accessKeyId}' already exists. Each client must have a unique Access Key Id.");
            }
        }

        private static void ValidateOAuthClient(Item item, SitecoreEventArgs scArgs)
        {
            var issuer = item.Fields[Templates.OAuthClient.Fields.AllowedIssuer]?.Value?.Trim();
            var clientIdsField = item.Fields[Templates.OAuthClient.Fields.OAuthClientIds];
            var rawClientIds = clientIdsField?.Value ?? string.Empty;

            // Parse lines, trim, drop empties, dedup (case-insensitive).
            var clientIds = rawClientIds
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Hard-reject rules.
            foreach (var cid in clientIds)
            {
                string reason = null;
                if (cid.IndexOfAny(new[] { '*', '?' }) >= 0)
                    reason = "wildcard";
                else if (cid.Length < MinClientIdLength)
                    reason = $"shorterThan{MinClientIdLength}Chars";
                else if (ReservedClientIds.Contains(cid))
                    reason = "reservedWord";

                if (reason != null)
                {
                    PowerShellLog.Error(
                        $"[RemotingClient] action=saveDenied reason=invalidClientId subReason={reason} remotingClient={item.Name} clientId={LogSanitizer.SanitizeValue(cid)}");
                    scArgs.Result.Cancel = true;
                    scArgs.Result.Messages.Add(
                        $"OAuth Client Ids entry '{cid}' is not allowed (reason: {reason}). Use a specific client_id of at least {MinClientIdLength} characters; no wildcards or placeholder words.");
                    return;
                }
            }

            // Normalize stored value: trimmed + deduped + consistent newlines.
            var normalized = string.Join("\r\n", clientIds);
            if (!string.Equals(normalized, rawClientIds, StringComparison.Ordinal))
            {
                clientIdsField.Value = normalized;
            }

            // Cross-item duplicate (issuer, client_id) check - ambiguous policy if two items match one token.
            if (!string.IsNullOrEmpty(issuer) && clientIds.Count > 0)
            {
                var duplicate = FindDuplicateIssuerClientId(item, issuer, clientIds);
                if (duplicate != null)
                {
                    PowerShellLog.Error(
                        $"[RemotingClient] action=saveDenied reason=duplicateIssuerClientId remotingClient={item.Name} clientId={LogSanitizer.SanitizeValue(duplicate.Item1)} duplicateOf={LogSanitizer.SanitizeValue(duplicate.Item2)}");
                    scArgs.Result.Cancel = true;
                    scArgs.Result.Messages.Add(
                        $"OAuth Client Id '{duplicate.Item1}' is already registered to '{duplicate.Item2}' for issuer '{issuer}'. An (issuer, client_id) pair must resolve to one client.");
                }
            }
        }

        private static bool HasDuplicateAccessKeyId(Item currentItem, string accessKeyId)
        {
            using (new SecurityDisabler())
            {
                var folder = currentItem.Parent;
                if (folder == null) return false;
                return HasDuplicateInChildren(folder, currentItem, accessKeyId);
            }
        }

        private static bool HasDuplicateInChildren(Item folder, Item excludeItem, string accessKeyId)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.ID == excludeItem.ID) continue;

                if (child.TemplateID == Templates.SharedSecretClient.Id)
                {
                    var childKeyId = child.Fields[Templates.SharedSecretClient.Fields.AccessKeyId]?.Value?.Trim();
                    if (string.Equals(childKeyId, accessKeyId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else if (child.HasChildren)
                {
                    if (HasDuplicateInChildren(child, excludeItem, accessKeyId))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static Tuple<string, string> FindDuplicateIssuerClientId(
            Item currentItem, string issuer, List<string> clientIds)
        {
            using (new SecurityDisabler())
            {
                var folder = currentItem.Parent;
                if (folder == null) return null;

                var seen = new HashSet<string>(clientIds, StringComparer.OrdinalIgnoreCase);
                return FindDuplicateInChildren(folder, currentItem, issuer, seen);
            }
        }

        private static Tuple<string, string> FindDuplicateInChildren(
            Item folder, Item excludeItem, string issuer, HashSet<string> candidateClientIds)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.ID == excludeItem.ID) continue;

                if (child.TemplateID == Templates.OAuthClient.Id)
                {
                    var childIssuer = child.Fields[Templates.OAuthClient.Fields.AllowedIssuer]?.Value?.Trim();
                    if (string.Equals(childIssuer, issuer, StringComparison.OrdinalIgnoreCase))
                    {
                        var childIds = (child.Fields[Templates.OAuthClient.Fields.OAuthClientIds]?.Value ?? string.Empty)
                            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => s.Length > 0);

                        foreach (var existingId in childIds)
                        {
                            if (candidateClientIds.Contains(existingId))
                            {
                                return Tuple.Create(existingId, child.Name);
                            }
                        }
                    }
                }
                else if (child.HasChildren)
                {
                    var dup = FindDuplicateInChildren(child, excludeItem, issuer, candidateClientIds);
                    if (dup != null) return dup;
                }
            }
            return null;
        }
    }
}
