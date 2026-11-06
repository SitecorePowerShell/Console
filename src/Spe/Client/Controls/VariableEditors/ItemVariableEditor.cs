using System;
using System.Collections;
using System.Web.UI;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.WebControls;
using Spe.Core.Extensions;
using Control = System.Web.UI.Control;

namespace Spe.Client.Controls.VariableEditors
{
    internal class ItemVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return valueType == typeof(Item) ||
                   (!string.IsNullOrEmpty(editor) && editor.HasWord("item"));
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var editor = variable["Editor"] as string;

            var item = value as Item;
            var source = variable["Source"] as string;
            var root = variable["Root"] as string;
            var sourceRoot = string.IsNullOrEmpty(source)
                ? "/sitecore"
                : StringUtil.ExtractParameter("DataSource", source);

            var dataContext = item != null
                ? new DataContext
                {
                    DefaultItem = item.Paths.Path,
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("dataContext"),
                    Parameters = string.IsNullOrEmpty(source) ? "databasename=" + item.Database.Name : source,
                    DataViewName = "Master",
                    Root = string.IsNullOrEmpty(root) ? sourceRoot : root,
                    Database = item.Database.Name,
                    Selected = new[] { new DataUri(item.ID, item.Language, item.Version) },
                    Folder = item.ID.ToString(),
                    Language = item.Language,
                    Version = item.Version
                }
                : new DataContext
                {
                    ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("dataContext"),
                    Parameters = string.IsNullOrEmpty(source) ? "databasename=master" : source,
                    DataViewName = "Master",
                    Root = string.IsNullOrEmpty(root) ? sourceRoot : root
                };

            context.DataContextPanel.Controls.Add(dataContext);

            var treePicker = new TreePicker
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                Value = item?.ID.ToString() ?? string.Empty,
                DataContext = dataContext.ID,
                AllowNone =
                    !string.IsNullOrEmpty(editor) &&
                    (editor.IndexOf("allownone", StringComparison.OrdinalIgnoreCase) > -1)
            };
            treePicker.Class += " treePicker";

            return treePicker;
        }

        public bool CanReadValue(Control control)
        {
            return control is TreePicker;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            var picker = (TreePicker)control;
            var contextId = picker.DataContext;
            var dataContext = (DataContext)context.DataContextPanel.FindControl(contextId);
            result.Add("Value", string.IsNullOrEmpty(picker.Value) ? null : dataContext.CurrentItem);
        }
    }
}
