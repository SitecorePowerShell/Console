using System;
using System.Collections.Generic;
using System.Management.Automation;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Links;
using Sitecore.Resources.Media;

namespace Cognifide.PowerShell.PowerShellIntegrations
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
            {"ProviderPath", "\"$($this.Database.Name):$($this.Paths.Path.Substring(9).Replace('/','\\'))\""}
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
                    string getter = item.Fields[field] != null && item.Fields[field].TypeKey == "datetime"
                        ? String.Format("[Sitecore.DateUtil]::IsoDateToDateTime($this[\"{0}\"])", field)
                        : String.Format("$this[\"{0}\"]", field);
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

        public static void Modify(PSObject powerShellItem, string propertyName, object[] value)
        {
            var item = powerShellItem.BaseObject as Item;
            if (item != null)
            {
                item.Edit(
                    args =>
                    {
                        object newValue = value[0].BaseObject();
                        if (newValue is DateTime)
                        {
                            item[propertyName] = ((DateTime) newValue).ToString("yyyyMMddTHHmmss");
                        }
                        else if (newValue is Item)
                        {
                            if (string.Equals(item.Fields[propertyName].TypeKey, "image",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                var media = new MediaItem(newValue as Item);
                                ImageField imageField = item.Fields[propertyName];

                                if (imageField.MediaID != media.ID)
                                {
                                    imageField.Clear();
                                    imageField.MediaID = media.ID;
                                    if (!String.IsNullOrEmpty(media.Alt))
                                    {
                                        imageField.Alt = media.Alt;
                                    }
                                    else
                                    {
                                        imageField.Alt = media.DisplayName;
                                    }
                                }
                            }
                            else if (string.Equals(item.Fields[propertyName].TypeKey, "general link",
                                StringComparison.OrdinalIgnoreCase))
                            {
                                Item newLink = newValue as Item;
                                LinkField linkField = item.Fields[propertyName];
                                linkField.Clear();
                                if (MediaManager.HasMediaContent(newLink))
                                {
                                    linkField.LinkType = "media";
                                    linkField.Url = newLink.Paths.MediaPath;
                                }
                                else
                                {
                                    linkField.LinkType = "internal";
                                    linkField.Url = newLink.Paths.ContentPath;
                                }
                                linkField.TargetID = newLink.ID;
                            }
                            else
                            {
                                item[propertyName] = (newValue as Item).ID.ToString();
                            }
                        }
                        else
                        {
                            item[propertyName] = newValue.ToString();
                        }
                    });
            }
        }
    }
}