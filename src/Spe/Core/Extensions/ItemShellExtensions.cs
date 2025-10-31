using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Common;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Resources.Media;
using Spe.Core.Data;
using Spe.Core.Settings;

namespace Spe.Core.Extensions
{
    public class ItemShellExtensions
    {
        private static readonly ConcurrentDictionary<ID, Dictionary<ID, string>> AllPropertySets = new();

        private static readonly HashSet<ID> StandardFields =
        [
            Sitecore.FieldIDs.ArchiveDate,
            Sitecore.FieldIDs.ArchiveVersionDate,
            Sitecore.FieldIDs.BaseTemplate,
            Sitecore.FieldIDs.SubitemsSorting,
            Sitecore.FieldIDs.Command,
            Sitecore.FieldIDs.CommentDialogHeight,
            Sitecore.FieldIDs.CommentTemplate,
            Sitecore.FieldIDs.ContextMenu,
            Sitecore.FieldIDs.Created,
            Sitecore.FieldIDs.CreatedBy,
            Sitecore.FieldIDs.DefaultDomain,
            Sitecore.FieldIDs.DefaultWorkflow,
            Sitecore.FieldIDs.DefaultCommentDialogHeight,
            Sitecore.FieldIDs.DefaultCommentTemplate,
            Sitecore.FieldIDs.DictionaryKey,
            Sitecore.FieldIDs.DictionaryPhrase,
            Sitecore.FieldIDs.DisplayName,
            Sitecore.FieldIDs.DomainRoleNameTemplate,
            Sitecore.FieldIDs.DomainUserNameTemplate,
            Sitecore.FieldIDs.DomainMembershipProvider,
            Sitecore.FieldIDs.DomainUniqueName,
            Sitecore.FieldIDs.EditorPath,
            Sitecore.FieldIDs.EnableLanguageFallback,
            Sitecore.FieldIDs.EnableSharedLanguageFallback,
            Sitecore.FieldIDs.EnforceVersionPresence,
            Sitecore.FieldIDs.EnableItemFallback,
            Sitecore.FieldIDs.FallbackDomain,
            Sitecore.FieldIDs.FallbackLanguage,
            Sitecore.FieldIDs.Hidden,
            Sitecore.FieldIDs.HideVersion,
            Sitecore.FieldIDs.Icon,
            Sitecore.FieldIDs.InheritSecurity, 
            Sitecore.FieldIDs.LanguageIso,
            Sitecore.FieldIDs.Lock,
            Sitecore.FieldIDs.Branches,
            Sitecore.FieldIDs.NextState,
            Sitecore.FieldIDs.NeverPublish,
            Sitecore.FieldIDs.Originator,
            Sitecore.FieldIDs.PageDefinition,
            Sitecore.FieldIDs.Presentation,
            Sitecore.FieldIDs.Preview,
            Sitecore.FieldIDs.PublishDate,
            Sitecore.FieldIDs.PublishingTargets,
            Sitecore.FieldIDs.PublishingTargetDatabase,
            Sitecore.FieldIDs.ReminderDate,
            Sitecore.FieldIDs.ReminderRecipients,
            Sitecore.FieldIDs.ReminderText,
            Sitecore.FieldIDs.Renderers,
            Sitecore.FieldIDs.Ribbon,
            Sitecore.FieldIDs.LayoutField,
            Sitecore.FieldIDs.ReadOnly,
            Sitecore.FieldIDs.Reference,
            Sitecore.FieldIDs.Revision,
            Sitecore.FieldIDs.Owner,
            Sitecore.FieldIDs.Security,
            Sitecore.FieldIDs.Skin,
            Sitecore.FieldIDs.Sortorder,
            Sitecore.FieldIDs.StandardValues,
            Sitecore.FieldIDs.StandardValueHolderId,
            Sitecore.FieldIDs.State,
            Sitecore.FieldIDs.AppearanceEvaluatorType,
            Sitecore.FieldIDs.Style,
            Sitecore.FieldIDs.Thumbnail,
            Sitecore.FieldIDs.UnpublishDate,
            Sitecore.FieldIDs.Updated,
            Sitecore.FieldIDs.UpdatedBy,
            Sitecore.FieldIDs.UserMembership,
            Sitecore.FieldIDs.ValidFrom,
            Sitecore.FieldIDs.ValidTo,
            Sitecore.FieldIDs.FinalLayoutField,
            Sitecore.FieldIDs.Workflow,
            Sitecore.FieldIDs.WorkflowState,
            Sitecore.FieldIDs.Source,
            Sitecore.FieldIDs.SourceItem,
            Sitecore.FieldIDs.UIStaticItem,
            Sitecore.FieldIDs.StandardFieldsID
        ];
        

        internal static PSObject GetPsObject(SessionState provider, Item item)
        {
            var psObject = PSObject.AsPSObject(item);

            if (Switcher<bool, DisablePropertyExpander>.CurrentValue) return psObject;

            if (!AllPropertySets.TryGetValue(item.TemplateID, out var propertySet))
            {
                item.Fields.ReadAll();
                propertySet = new Dictionary<ID, string>(item.Fields.Count);
                foreach (Field field in item.Fields)
                {
                    if (field.Name != "ID"&& !StandardFields.Contains(field.ID)) // don't map - native property
                    {
                        propertySet.Add(field.ID, field.Name);
                    }
                }
                AllPropertySets.TryAdd(item.TemplateID, propertySet);
            }

            var typedFieldGetter = new CustomFieldAccessor(item);
            psObject.Members.Add(new PSNoteProperty("_", typedFieldGetter));
            psObject.Members.Add(new PSNoteProperty("PSFields", typedFieldGetter));

            foreach (var fieldKey in propertySet.Keys)
            {
                var fieldName = propertySet[fieldKey];
                var fieldId = fieldKey;

                if (string.IsNullOrEmpty(fieldName)) continue;

                while (psObject.Members[fieldName] != null)
                {
                    fieldName = "_" + fieldName;
                }
                
                if (item.Fields[fieldId] != null)
                {
                    psObject.Members.Add(new PsItemProperty(item, fieldName, item.Fields[fieldId]));
                }
            }
            return psObject;
        }

        public static void Modify(Item item, string propertyName, object[] value)
        {
            ModifyProperty(item, propertyName, value[0].BaseObject());
        }

        public static void ModifyProperty(Item item, string propertyName, object value)
        {
            if (item != null)
            {
                ModifyProperty(item, item.Fields[propertyName].ID, value);
            }
        }
        
        public static void ModifyProperty(Item item, ID fieldId, object value)
        {
            item?.Edit(
                args =>
                {
                    var newValue = value.BaseObject();
                    var field = FieldTypeManager.GetField(item.Fields[fieldId]);

                    if ((newValue as object[])?[0].BaseObject() is Item)
                    {
                        newValue =
                            (newValue as object[]).Select(p => p.BaseObject())
                            .Where(p => p is Item)
                            .Cast<Item>()
                            .ToList();
                    }
                    if (newValue is Item)
                    {
                        newValue = new List<Item> { newValue as Item };
                    }

                    switch (newValue)
                    {
                        case List<Item> items:
                            {
                                var lastItem = items.Last();
                                switch (field)
                                {
                                    case ImageField imageField:
                                        {
                                            var media = new MediaItem(lastItem);

                                            if (imageField.MediaID == media.ID) return;

                                            imageField.Clear();
                                            imageField.MediaID = media.ID;
                                            imageField.Alt = !string.IsNullOrEmpty(media.Alt) ? media.Alt : media.DisplayName;
                                            break;
                                        }
                                    case LinkField linkField:
                                        {
                                            linkField.Clear();

                                            if (MediaManager.HasMediaContent(lastItem))
                                            {
                                                linkField.LinkType = "media";
                                                linkField.Url = lastItem.Paths.MediaPath;
                                            }
                                            else
                                            {
                                                linkField.LinkType = "internal";
                                                linkField.Url = lastItem.Paths.ContentPath;
                                            }
                                            linkField.TargetID = lastItem.ID;
                                            break;
                                        }
                                    case MultilistField multilistField:
                                        {
                                            multilistField.Value = string.Empty;
                                            foreach (var linkedItem in items)
                                                multilistField.Add(linkedItem.ID.ToString());
                                            break;
                                        }
                                    case FileField _ when !MediaManager.HasMediaContent(lastItem):
                                        return;
                                    case FileField fileField:
                                        fileField.Clear();
                                        fileField.MediaID = lastItem.ID;
                                        fileField.Src = MediaManager.GetMediaUrl(lastItem);
                                        break;
                                    case ValueLookupField _:
                                        field.Value = lastItem.Name;
                                        break;
                                    // LookupField, GroupedDroplinkField, ReferenceField, Other
                                    default:
                                        field.Value = lastItem.ID.ToString();
                                        break;
                                }

                                break;
                            }
                        case DateTime time:
                            item[fieldId] = time.ToString("yyyyMMddTHHmmss");
                            break;
                        case bool b:
                            item[fieldId] = b ? "1" : "";
                            break;
                        default:
                            item[fieldId] = newValue?.ToString();
                            break;
                    }
                });
        }

        public static PSObject WrapInItemOwner(SessionState provider, Item item, object o)
        {
            var psObject = PSObject.AsPSObject(o);
            if (item == null || provider == null || o == null) return psObject;

            psObject.Properties.Add(new PSScriptProperty(
                "OwnerItemId", provider.InvokeCommand.NewScriptBlock($"'{{{item.ID}}}'")));
            psObject.Properties.Add(new PSScriptProperty(
                "OwnerItemPath",
                provider.InvokeCommand.NewScriptBlock(
                    $"\"{item.Database.Name}:{item.Paths.Path.Substring(9).Replace('/', '\\')}\"")));
            return psObject;
        }

        internal void TemplateFieldsInvalidateCheck(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            if (Event.ExtractParameter(args, 0) is Item item && item.Paths.Path.StartsWith(ApplicationSettings.TemplatesPath, StringComparison.OrdinalIgnoreCase))
            {
                AllPropertySets.Clear();
            }
        }

        internal void TemplateFieldsInvalidateCheckRemote(object sender, EventArgs args)
        {
            if (EventDisabler.IsActive) return;

            Assert.ArgumentNotNull(args, "args");
            var isreErgs = args as ItemSavedRemoteEventArgs;
            if (isreErgs?.Item != null &&
                isreErgs.Item.Paths.Path.StartsWith(ApplicationSettings.TemplatesPath, StringComparison.OrdinalIgnoreCase))
            {
                AllPropertySets.Clear();
            }
        }
    }
}