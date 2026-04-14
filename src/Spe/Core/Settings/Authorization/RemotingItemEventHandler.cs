using System;
using System.Collections.Generic;
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
    /// Event handler for remoting security items (API keys and policies).
    /// Invalidates caches on save and validates API key constraints on saving.
    /// </summary>
    public class RemotingItemEventHandler
    {
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
            else if (item.TemplateID == Templates.RemotingApiKey.Id)
            {
                HttpRuntime.Cache.Remove("Spe.RemotingApiKeys");
                HttpRuntime.Cache.Remove("Spe.RemotingApiKeys.ByAccessKeyId");
            }
        }

        /// <summary>
        /// Validates API key constraints before save.
        /// Rejects duplicate Access Key Ids across all API key items.
        /// </summary>
        internal void OnItemSaving(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            if (!(args is SitecoreEventArgs scArgs) || scArgs.Parameters.Length < 1) return;

            var item = scArgs.Parameters[0] as Item;
            if (item == null || item.TemplateID != Templates.RemotingApiKey.Id) return;

            var accessKeyId = item.Fields[Templates.RemotingApiKey.Fields.AccessKeyId]?.Value?.Trim();
            if (string.IsNullOrEmpty(accessKeyId)) return;

            if (HasDuplicateAccessKeyId(item, accessKeyId))
            {
                PowerShellLog.Error(
                    $"[ApiKey] action=saveDenied reason=duplicateAccessKeyId key={item.Name} accessKeyId={accessKeyId}");
                scArgs.Result.Cancel = true;
                scArgs.Result.Messages.Add(
                    $"An API key with Access Key Id '{accessKeyId}' already exists. Each key must have a unique Access Key Id.");
            }
        }

        private static bool HasDuplicateAccessKeyId(Item currentItem, string accessKeyId)
        {
            using (new SecurityDisabler())
            {
                var keysFolder = currentItem.Parent;
                if (keysFolder == null) return false;

                return HasDuplicateInChildren(keysFolder, currentItem, accessKeyId);
            }
        }

        private static bool HasDuplicateInChildren(Item folder, Item excludeItem, string accessKeyId)
        {
            foreach (Item child in folder.GetChildren())
            {
                if (child.ID == excludeItem.ID) continue;

                if (child.TemplateID == Templates.RemotingApiKey.Id)
                {
                    var childKeyId = child.Fields[Templates.RemotingApiKey.Fields.AccessKeyId]?.Value?.Trim();
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
    }
}
