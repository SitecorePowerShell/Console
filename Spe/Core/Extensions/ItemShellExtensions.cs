using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Resources.Media;
using Spe.Core.Settings;

namespace Spe.Core.Extensions
{
    public class ItemShellExtensions
    {
        private static readonly Dictionary<ID, Dictionary<ID, string>> AllPropertySets =
            new Dictionary<ID, Dictionary<ID, string>>();

        private static readonly string HelperClassName = typeof(ItemShellExtensions).FullName;

        private static readonly Dictionary<string, string> CustomGetters = new Dictionary<string, string>
        {
            {"ItemPath", "$this.Paths.Path"},
            {"FullPath", "$this.Paths.FullPath"},
            {"MediaPath", "$this.Paths.MediaPath"},
            {"ContentPath", "$this.Paths.ContentPath"},
            {"ProviderPath", "[Spe.Core.Utility.PathUtilities]::GetProviderPath($this)"},
            {"BaseTemplate", "[Sitecore.Data.Managers.TemplateManager]::GetTemplate($this).GetBaseTemplates()"}
        };

        internal static PSObject GetPsObject(SessionState provider, Item item)
        {
            var psObject = PSObject.AsPSObject(item);

            Dictionary<ID, string> propertySet;
            if (AllPropertySets.ContainsKey(item.TemplateID))
            {
                propertySet = AllPropertySets[item.TemplateID];
            }
            else
            {
                item.Fields.ReadAll();
                propertySet = new Dictionary<ID, string>(item.Fields.Count);
                foreach (Field field in item.Fields)
                {
                    if (field.Name != "ID") // don't map - native property
                    {
                        propertySet.Add(field.ID, field.Name);
                    }
                }
                AllPropertySets.Add(item.TemplateID, propertySet);
            }

            var typedFieldGetter = new CustomFieldAccessor(item);
            psObject.Properties.Add(new PSNoteProperty("_", typedFieldGetter));
            psObject.Properties.Add(new PSNoteProperty("PSFields", typedFieldGetter));

            foreach (var fieldKey in propertySet.Keys)
            {
                var fieldName = propertySet[fieldKey];
                var fieldId = fieldKey;

                if (string.IsNullOrEmpty(fieldName)) continue;

                while (psObject.Properties[fieldName] != null)
                {
                    fieldName = "_" + fieldName;
                }

                var getter = $"$this[\"{fieldId}\"]";
                if (item.Fields[fieldId] != null)
                {
                    if (item.Fields[fieldId].TypeKey == "datetime")
                    {
                        getter = $"[Sitecore.DateUtil]::IsoDateToDateTime($this[\"{fieldId}\"])";
                    }
                }
                var setter =
                    $"[{HelperClassName}]::Modify($this, \"{fieldId}\", $Args );";

                psObject.Properties.Add(new PSScriptProperty(
                    fieldName,
                    provider.InvokeCommand.NewScriptBlock(getter),
                    provider.InvokeCommand.NewScriptBlock(setter)));
            }

            foreach (var customGetter in CustomGetters.Keys)
            {
                if (psObject.Properties[customGetter] == null)
                {
                    psObject.Properties.Add(new PSScriptProperty(
                        customGetter,
                        provider.InvokeCommand.NewScriptBlock(CustomGetters[customGetter])));
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
            item?.Edit(
                args =>
                {
                    var newValue = value.BaseObject();
                    var field = FieldTypeManager.GetField(item.Fields[propertyName]);

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
                        newValue = new List<Item> {newValue as Item};
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
                            item[propertyName] = time.ToString("yyyyMMddTHHmmss");
                            break;
                        case bool b:
                            item[propertyName] = b ? "1" : "";
                            break;
                        default:
                            item[propertyName] = newValue?.ToString();
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
            Assert.ArgumentNotNull(args, "args");
            if (Event.ExtractParameter(args, 0) is Item item && item.Paths.Path.StartsWith(ApplicationSettings.TemplatesPath, StringComparison.OrdinalIgnoreCase))
            {
                AllPropertySets.Clear();
            }
        }

        internal void TemplateFieldsInvalidateCheckRemote(object sender, EventArgs args)
        {
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