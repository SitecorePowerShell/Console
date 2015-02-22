using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;

namespace Cognifide.PowerShell.Core.Extensions
{
    public static class ItemShellExtensions
    {
        private static readonly Dictionary<ID, List<string>> allPropertySets = new Dictionary<ID, List<string>>();

        private static readonly Dictionary<string, string> customGetters = new Dictionary<string, string>
        {
            {"ItemPath", "$this.Paths.Path"},
            {"FullPath", "$this.Paths.FullPath"},
            {"MediaPath", "$this.Paths.MediaPath"},
            {"ContentPath", "$this.Paths.ContentPath"},
            {"ProviderPath", "[Cognifide.PowerShell.Core.Utility.PathUtilities]::GetItemPsPath($this)"}
        };

        //internal static PSObject GetPSObject(CmdletProvider provider, Item item)
        internal static PSObject GetPsObject(SessionState provider, Item item)
        {
            PSObject psobj = PSObject.AsPSObject(item);

            List<string> propertySet;
            if (allPropertySets.ContainsKey(item.TemplateID))
            {
                propertySet = allPropertySets[item.TemplateID];
            }
            else
            {
                item.Fields.ReadAll();
                propertySet = new List<string>(item.Fields.Count);
                foreach (Field field in item.Fields)
                {
                    if (field.Name != "ID") // don't map - native property
                    {
                        propertySet.Add(field.Name);
                    }
                }
                allPropertySets.Add(item.TemplateID, propertySet);
            }

            foreach (var field in propertySet)
            {
                if (!String.IsNullOrEmpty(field))
                {
                    bool duplicate = psobj.Properties[field] == null;

                    string getter = String.Format("$this[\"{0}\"]", field);
                    if (item.Fields[field] != null)
                    {
                        switch (item.Fields[field].TypeKey)
                        {
                            case ("datetime"):
                                getter = String.Format("[Sitecore.DateUtil]::IsoDateToDateTime($this[\"{0}\"])", field);
                                break;
                            default:
                                getter = String.Format("$this[\"{0}\"]", field);
                                break;
                        }
                    }
                    string setter =
                            String.Format("[{0}]::Modify($this, \"{1}\", $Args );",
                                typeof (ItemShellExtensions).FullName, field);

                    psobj.Properties.Add(new PSScriptProperty(
                        duplicate ? field : String.Format("_{0}", field),
                        provider.InvokeCommand.NewScriptBlock(getter),
                        provider.InvokeCommand.NewScriptBlock(setter)));
                }
            }

            foreach (var customGetter in customGetters.Keys)
            {
                psobj.Properties.Add(new PSScriptProperty(
                    customGetter,
                    provider.InvokeCommand.NewScriptBlock(customGetters[customGetter])));
            }


            return psobj;
        }

        public static void Modify(Item item, string propertyName, object[] value)
        {
            ModifyProperty(item, propertyName, value[0].BaseObject());
        }

        public static void ModifyProperty(Item item, string propertyName, object value)
        {
            if (item != null)
            {
                item.Edit(
                    args =>
                    {                                                
                        object newValue = value.BaseObject();
                        CustomField field = FieldTypeManager.GetField(item.Fields[propertyName]);

                        if (newValue is object[] && (newValue as object[])[0].BaseObject() is Item)
                        {
                            newValue = (newValue as object[]).Select(p => PowerShellExtensions.BaseObject(p)).Where(p => p is Item).Cast<Item>().ToList();
                        }
                        if (newValue is Item)
                        {
                            newValue = new List<Item> {newValue as Item};
                        }

                        if (newValue is List<Item>)
                        {
                            var items = newValue as List<Item>;
                            var lastItem = items.Last();
                            if (field is ImageField)
                            {
                                var media = new MediaItem(lastItem);
                                var imageField = field as ImageField;

                                if (imageField.MediaID != media.ID)
                                {
                                    imageField.Clear();
                                    imageField.MediaID = media.ID;
                                    imageField.Alt = !String.IsNullOrEmpty(media.Alt) ? media.Alt : media.DisplayName;
                                }
                            }
                            else if (field is LinkField)
                            {
                                LinkField linkField = field as LinkField;
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
                            }
                            else if (field is MultilistField)
                            {
                                MultilistField linkField = field as MultilistField;
                                linkField.Value = string.Empty;
                                foreach (var linkedItem in items)
                                    linkField.Add(linkedItem.ID.ToString());
                            }
                            else if (field is FileField)
                            {
                                var linkField = field as FileField;

                                if (MediaManager.HasMediaContent(lastItem))
                                {
                                    linkField.Clear();
                                    linkField.MediaID = lastItem.ID;
                                    linkField.Src = MediaManager.GetMediaUrl(lastItem);
                                }
                            }
                            else if (field is ValueLookupField)
                            {
                                field.Value = lastItem.Name;
                            }
                            else // LookupField, GroupedDroplinkField, ReferenceField, Other
                            {
                                field.Value = lastItem.ID.ToString();
                            }
                        }
                        else if (newValue is DateTime)
                        {
                            item[propertyName] = ((DateTime)newValue).ToString("yyyyMMddTHHmmss");
                        }
                        else if (newValue is bool)
                        {
                            item[propertyName] = ((bool)newValue) ? "1" : "";
                        }
                        else
                        {
                            item[propertyName] = newValue.ToString();
                        }
                    });
            }
        }

        public static PSObject WrapInItemOwner(SessionState provider, Item item, object o)
        {
            PSObject psobj = PSObject.AsPSObject(o);
            if (item != null && provider != null && o != null)
            {
                psobj.Properties.Add(new PSScriptProperty(
                    "OwnerItemId", provider.InvokeCommand.NewScriptBlock(string.Format("'{{{0}}}'", item.ID))));
                psobj.Properties.Add(new PSScriptProperty(
                    "OwnerItemPath",
                    provider.InvokeCommand.NewScriptBlock(string.Format("\"{0}:{1}\"", item.Database.Name,
                        item.Paths.Path.Substring(9).Replace('/', '\\')))));
            }
            return psobj;
            
        }
    }
}