using System;
using System.Collections.Generic;
using System.Linq;
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
