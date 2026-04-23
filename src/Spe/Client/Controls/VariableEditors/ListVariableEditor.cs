using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Spe.Core.Extensions;
using Spe.Integrations.Processors;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class ListVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return !string.IsNullOrEmpty(editor) &&
                   editor.HasWord("treelist", "multiroottreelist", "multilist", "droplist", "droplink", "droptree",
                       "groupeddroplink", "groupeddroplist");
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var editor = variable["Editor"] as string;

            Item item = null;
            var strValue = string.Empty;

            if (value is Item)
            {
                item = (Item)value;
                strValue = item.ID.ToString();
            }
            else if (value is IEnumerable<object>)
            {
                var items = (value as IEnumerable<object>).Cast<Item>().ToList();
                item = items.FirstOrDefault();
                strValue = string.Join("|", items.Select(i => i.ID.ToString()).ToArray());
            }

            var dbName = item == null ? Sitecore.Context.ContentDatabase.Name : item.Database.Name;

            if (editor.HasWord("multilist"))
            {
                if (editor.HasWord("search"))
                {
                    var bucketlist = new BucketListExtended
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Value = strValue,
                        Database = dbName,
                        ItemID = Sitecore.ItemIDs.RootID.ToString(),
                        Source = variable["Source"] as string ?? "/sitecore"
                    };

                    bucketlist.Class += "  treePicker";

                    return bucketlist;
                }

                var multiList = new MultilistExtended
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Value = strValue,
                    Database = dbName,
                    ItemID = Sitecore.ItemIDs.RootID.ToString(),
                    Source = variable["Source"] as string ?? "/sitecore"
                };
                multiList.SetLanguage(item?.Language.Name ?? Sitecore.Context.Language.Name);

                multiList.Class += "  treePicker";

                return multiList;
            }

            if (editor.HasWord("droplist", "droplink", "groupeddroplink", "groupeddroplist"))
            {
                if (Sitecore.Context.ContentDatabase?.Name != dbName)
                {
                    return new Literal
                    {
                        Text = "<span style='color: red'>" +
                               Translate.Text(
                                   Texts
                                       .PowerShellMultiValuePrompt_GetVariableEditor_DropList_control_cannot_render_items_from_the_database___0___because_it_its_not_the_same_as___1___which_is_the_current_content_database__,
                                   dbName, Sitecore.Context.ContentDatabase?.Name) + "</span>",
                        Class = "varHint"
                    };
                }

                var lookupSource = variable["Source"] as string ?? "/sitecore";
                var scriptedItems = BaseScriptedDataSource.IsScripted(lookupSource)
                    ? BaseScriptedDataSource.RunEnumeration(lookupSource, item).ToArray()
                    : null;

                var allowNone = editor.IndexOf("allownone", StringComparison.OrdinalIgnoreCase) > -1;
                var placeholder = variable["Placeholder"] as string ?? string.Empty;

                if (editor.HasWord("groupeddroplist"))
                {
                    var groupedDroplist = new GroupedDroplistExtended
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Database = dbName,
                        ItemID = item?.ID.ToString() ?? Sitecore.ItemIDs.RootID.ToString(),
                        Source = lookupSource,
                        ItemLanguage = item?.Language.Name ?? Sitecore.Context.Language.Name,
                        Value = item?.Name ?? string.Empty,
                        ScriptedItems = scriptedItems,
                        AllowNone = allowNone,
                        Placeholder = placeholder
                    };

                    return groupedDroplist;
                }

                if (editor.HasWord("groupeddroplink"))
                {
                    var groupedDroplink = new GroupedDroplinkExtended
                    {
                        ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                        Database = dbName,
                        ItemID = item?.ID.ToString() ?? Sitecore.ItemIDs.RootID.ToString(),
                        Source = lookupSource,
                        ItemLanguage = item?.Language.Name ?? Sitecore.Context.Language.Name,
                        Value = allowNone
                            ? (item?.ID.ToString() ?? string.Empty)
                            : (item?.ID.ToString() ?? Sitecore.ItemIDs.RootID.ToString()),
                        ScriptedItems = scriptedItems,
                        AllowNone = allowNone,
                        Placeholder = placeholder
                    };

                    return groupedDroplink;
                }

                var lookup = new LookupExExtended
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Database = dbName,
                    ItemID = item?.ID.ToString() ?? Sitecore.ItemIDs.RootID.ToString(),
                    Source = lookupSource,
                    ItemLanguage = item?.Language.Name ?? Sitecore.Context.Language.Name,
                    Value = allowNone
                        ? (item?.ID.ToString() ?? string.Empty)
                        : (item?.ID.ToString() ?? Sitecore.ItemIDs.RootID.ToString()),
                    ScriptedItems = scriptedItems,
                    AllowNone = allowNone,
                    Placeholder = placeholder
                };
                lookup.Class += " textEdit";

                return lookup;
            }

            if (editor.HasWord("droptree"))
            {
                var tree = new Tree
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Database = dbName,
                    ItemID = item?.ID.ToString() ?? Sitecore.ItemIDs.Null.ToString(),
                    Source = variable["Source"] as string ?? "",
                    Value = item?.ID.ToString() ?? "",
                    ItemLanguage = item?.Language.Name ?? Sitecore.Context.Language.Name
                };
                tree.Class += " textEdit";

                return tree;
            }

            if (editor.HasWord("multiroottreelist"))
            {
                var source = variable["Source"] as string ?? "/sitecore";

                if (BaseScriptedDataSource.IsScripted(source))
                {
                    var rootItems = BaseScriptedDataSource.RunEnumeration(source, item);
                    var rootIds = string.Join("|", rootItems.Select(i => i.ID.ToString()));
                    source = "DataSource=" + rootIds;
                }

                var multiRootTreeList = new MultiRootTreeList
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                    Value = strValue,
                    AllowMultipleSelection = true,
                    DatabaseName = dbName,
                    Source = source,
                    DisplayFieldName = variable["DisplayFieldName"] as string ?? "__DisplayName"
                };

                multiRootTreeList.Class += " treePicker";

                return multiRootTreeList;
            }

            var treeList = new TreelistExtended
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Value = strValue,
                AllowMultipleSelection = true,
                DatabaseName = dbName,
                Source = variable["Source"] as string ?? "/sitecore",
                DisplayFieldName = variable["DisplayFieldName"] as string ?? "__DisplayName"
            };

            treeList.Class += " treePicker";

            return treeList;
        }

        public bool CanReadValue(Control control)
        {
            return control is TreeList ||
                   control is MultilistEx ||
                   control is GroupedDroplist ||
                   control is GroupedDroplink ||
                   control is LookupEx;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            if (control is TreeList treeList)
            {
                var strIds = treeList.GetValue();
                var ids = strIds.Split('|');
                var db = string.IsNullOrEmpty(treeList.DatabaseName)
                    ? Sitecore.Context.ContentDatabase
                    : Database.GetDatabase(treeList.DatabaseName);
                var items = ids.Select(p => db.GetItem(p)).ToList();
                result.Add("Value", items);
            }
            else if (control is MultilistEx multilist)
            {
                var strIds = multilist.GetValue();
                var ids = strIds.Split('|');
                var items = ids.Select(p => Sitecore.Context.ContentDatabase.GetItem(p)).ToList();
                result.Add("Value", items);
            }
            else if (control is GroupedDroplist groupedDroplist)
            {
                result.Add("Value",
                    !string.IsNullOrEmpty(groupedDroplist.Value)
                        ? groupedDroplist.Value
                        : null);
            }
            else if (control is GroupedDroplink groupedDroplink)
            {
                result.Add("Value",
                    !string.IsNullOrEmpty(groupedDroplink.Value)
                        ? Sitecore.Context.ContentDatabase.GetItem(groupedDroplink.Value)
                        : null);
            }
            else if (control is LookupEx lookup)
            {
                result.Add("Value",
                    !string.IsNullOrEmpty(lookup.Value)
                        ? Sitecore.Context.ContentDatabase.GetItem(lookup.Value)
                        : null);
            }
        }
    }
}
