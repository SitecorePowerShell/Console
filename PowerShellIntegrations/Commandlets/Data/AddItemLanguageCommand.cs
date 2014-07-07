using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.Publishing.Pipelines.PublishItem;
using Sitecore.Shell.Framework.Commands;

namespace Cognifide.PowerShell.PowerShellIntegrations.Commandlets.Data
{
    [Cmdlet(VerbsCommon.Add, "ItemLanguage")]
    [OutputType(new[] { typeof(Item) }, ParameterSetName = new[] { "Item from Pipeline", "Item from Path", "Item from ID" })]
    public class AddItemLanguageCommand : BaseItemCommand
    {
        public enum ActionIfExists
        {
            Skip,
            Append,
            OverwriteLatest
        }

        [Parameter]
        public SwitchParameter Recurse { get; set; }

        [Parameter]
        public ActionIfExists IfExist { get; set; }

        [Parameter]
        public string[] TargetLanguage { get; set; }

        [Parameter]
        public SwitchParameter DoNotCopyFields { get; set; }

        [Parameter]
        public string[] IgnoredFields { get; set; }

        private static readonly HashSet<string> configIgnoredFields =
            new HashSet<string>(Factory.GetStringSet("powershell/translation/ignoredFields/field"));

        private HashSet<string> ignoredFields;

        protected override void BeginProcessing()
        {
            ignoredFields = new HashSet<string>(configIgnoredFields);
            if (IgnoredFields != null)
            {
                foreach (var ignoredField in IgnoredFields)
                {
                    ignoredFields.Add(ignoredField);                    
                }
            }
        }

        protected override void ProcessItem(Item item)
        {
            foreach (string targetLanguage in TargetLanguage)
            {
                Item latestVersion = item.Versions.GetLatestVersion(LanguageManager.GetLanguage(targetLanguage));
                if (IfExist != ActionIfExists.Skip || (latestVersion.Versions.Count == 0))
                {
                    CopyFields(item, latestVersion, false);
                }
                if (Recurse)
                {
                    foreach (Item childItem in item.Children)
                    {
                        ProcessItem(childItem);
                    }
                }
            }

        }

        public void CopyFields(Item sourceItem, Item targetItem, bool itemWasCreated)
        {
            try
            {
                if (sourceItem.Versions.Count > 0)
                {
                    if (targetItem.Versions.Count == 0 || IfExist == ActionIfExists.Append)
                    {
                        targetItem = targetItem.Versions.AddVersion();
                    }

                    if (!DoNotCopyFields)
                    {
                        targetItem.Editing.BeginEdit();
                        sourceItem.Fields.ReadAll();
                        foreach (Field field in sourceItem.Fields)
                        {
                            if (ShouldProcessField(field, itemWasCreated))
                            {
                                targetItem.Fields[field.Name].SetValue(field.Value, true);
                            }
                        }
                        targetItem.Editing.EndEdit();
                        targetItem.Editing.AcceptChanges();
                    }
                    WriteItem(targetItem);
                }
            }
            catch (Exception exception)
            {
                targetItem.Editing.CancelEdit();
                WriteError(new ErrorRecord(exception,"sitecore_item_translation_field_copy_error",ErrorCategory.NotSpecified, null));
            }
        }

        public bool ShouldProcessField(Field field, bool allowCopyShared)
        {
            if (field.ContainsStandardValue)
            {
                return false;
            }
            return !ignoredFields.Contains(field.Name, StringComparer.OrdinalIgnoreCase) &&
                   (allowCopyShared || !field.Shared);
        }
    }
}