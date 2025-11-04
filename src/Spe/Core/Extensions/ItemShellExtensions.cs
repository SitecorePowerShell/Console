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

        public static readonly HashSet<ID> StandardFields =
        [
            Sitecore.FieldIDs.ArchiveDate,
            Sitecore.FieldIDs.ArchiveVersionDate,
            Sitecore.FieldIDs.SubitemsSorting,
            Sitecore.FieldIDs.ContextMenu,
            Sitecore.FieldIDs.Created,
            Sitecore.FieldIDs.CreatedBy,
            Sitecore.FieldIDs.DefaultWorkflow,
            Sitecore.FieldIDs.DisplayName,
            Sitecore.FieldIDs.EnforceVersionPresence,
            Sitecore.FieldIDs.EnableItemFallback,
            Sitecore.FieldIDs.Hidden,
            Sitecore.FieldIDs.HideVersion,
            Sitecore.FieldIDs.Icon,
            Sitecore.FieldIDs.Lock,
            Sitecore.FieldIDs.Branches,
            Sitecore.FieldIDs.NeverPublish,
            Sitecore.FieldIDs.Originator,
            Sitecore.FieldIDs.Preview,
            Sitecore.FieldIDs.PublishDate,
            Sitecore.FieldIDs.PublishingTargets,
            Sitecore.FieldIDs.ReminderDate,
            Sitecore.FieldIDs.ReminderRecipients,
            Sitecore.FieldIDs.ReminderText,
            Sitecore.FieldIDs.Renderers,
            Sitecore.FieldIDs.Ribbon,
            Sitecore.FieldIDs.LayoutField,
            Sitecore.FieldIDs.ReadOnly,
            Sitecore.FieldIDs.Revision,
            Sitecore.FieldIDs.Owner,
            Sitecore.FieldIDs.Security,
            Sitecore.FieldIDs.Skin,
            Sitecore.FieldIDs.Sortorder,
            Sitecore.FieldIDs.StandardValues,
            Sitecore.FieldIDs.State,
            Sitecore.FieldIDs.Style,
            Sitecore.FieldIDs.Thumbnail,
            Sitecore.FieldIDs.UnpublishDate,
            Sitecore.FieldIDs.Updated,
            Sitecore.FieldIDs.UpdatedBy,
            Sitecore.FieldIDs.ValidFrom,
            Sitecore.FieldIDs.ValidTo,
            Sitecore.FieldIDs.FinalLayoutField,
            Sitecore.FieldIDs.Workflow,
            Sitecore.FieldIDs.WorkflowState,
            Sitecore.FieldIDs.Source,
            Sitecore.FieldIDs.SourceItem,
            new("{3607F9C7-DDA3-43C3-9720-39A7A5B3A4C3}"), // __Default View
            new("{C7815F60-96E1-40CB-BB06-B5F833F73B61}"), // __Persistent Bucket Filter
            new("{9541E67D-CE8C-4225-803D-33F7F29F09EF}"), // __Short description
            new("{C9283D9E-7C29-4419-9C28-5A5C8FF53E84}"), // __Bucketable
            new("{8C181989-2794-4B28-8EE4-6BB5CB928DC2}"), // __Boosting Rules
            new("{83798D75-DF25-4C28-9327-E8BAC2B75292}"), // __Insert Rules
            new("{B7E5B151-B145-4CED-85C5-FBDB566DFA4D}"), // __Validator Bar Validation Rules
            new("{93D1B217-B8F4-462E-BABF-68298C9CE667}"), // __Boost
            new("{57CBCA4C-8C94-446C-B8CA-7D8DC54F4285}"), // __Validate Button Validation Rules
            new("{4C9312A5-2E4E-42F8-AB6F-B8DB8B82BF22}"), // __Controller
            new("{D312103C-B36C-4CA5-864A-C85F9ABDA503}"), // __Is Bucket
            new("{A14F1B0C-4384-49EC-8790-28A440F3670C}"), // __Semantics
            new("{86B52EEF-078E-4D9E-80BF-888287070E6C}"), // __Workflow Validation Rules
            new("{F7B94D8C-A842-49F8-AB7A-2169D00426B0}"), // __Should Not Organize In Bucket
            new("{9DAFCA1D-D618-4616-86B8-A8ACD6B28A63}"), // __Bucket Parent Reference
            new("{21F74F6E-42D4-42A2-A4B4-4CEFBCFBD2BB}"), // __Facets
            new("{C2F5B2B5-71C1-431E-BF7F-DBDC1E5A2F83}"), // __Quick Action Bar Validation Rules
            new("{56776EDF-261C-4ABC-9FE7-70C618795239}"), // __Help link
            new("{577F1689-7DE4-4AD2-A15F-7FDC1759285F}"), // __Long description
            new("{9857F526-390F-48DF-B6D1-1A97CC328E8F}"), // __Version Name
            new("{F2DB8BA1-E477-41F5-8EF5-22EEFA8D2F6E}"), // __Enabled Views
            new("{A0CB3965-8884-4C7A-8815-B6B2E5CED162}"), // __Editors
            new("{9FB734CC-8952-4072-A2D4-40F890E16F56}"), // __Controller Action
            new("{4A749557-242B-4372-8A20-B2DB9D406301}"), // __AutoThumbnails
            new("{F47C0D78-61F9-479C-96DF-1159727D32C6}"), // __Suppressed Validation Rules
            new("{C0E276BB-8807-40AA-8138-E5C38B0C5DAB}"), // __Quick Actions
            new("{AC51462C-8A8D-493B-9492-34D1F26F20F1}"), // __Default Bucket Query
            new("{A4879E42-0270-458D-9C19-A20AF3C2B765}"), // __Presets
            new("{D85DB4EC-FF89-4F9C-9E7C-A9E0654797FC}"), // __Editor            
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