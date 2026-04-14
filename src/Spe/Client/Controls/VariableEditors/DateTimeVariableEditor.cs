using System;
using System.Collections;
using System.Web.UI;
using Sitecore;
using Sitecore.Data.Items;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;
using Control = System.Web.UI.Control;
using DateTime = System.DateTime;

namespace Spe.Client.Controls.VariableEditors
{
    internal class DateTimeVariableEditor : IVariableEditor
    {
        public bool CanHandle(IDictionary variable, string editor, Type valueType)
        {
            return valueType == typeof(DateTime) ||
                   (!string.IsNullOrEmpty(editor) && editor.HasWord("date", "time"));
        }

        public Control CreateControl(IDictionary variable, VariableEditorContext context)
        {
            var value = variable["Value"].BaseObject();
            var name = (string)variable["Name"];
            var editor = variable["Editor"] as string;

            var dateTimePicker = new Sitecore.Web.UI.HtmlControls.DateTimePicker
            {
                ID = Sitecore.Web.UI.HtmlControls.Control.GetUniqueID("variable_" + name + "_"),
                ShowTime = (variable["ShowTime"] != null && (bool)variable["ShowTime"]) ||
                           (!string.IsNullOrEmpty(editor) &&
                            editor.IndexOf("time", StringComparison.OrdinalIgnoreCase) > -1)
            };

            if (value is DateTime date)
            {
                if (date != DateTime.MinValue && date != DateTime.MaxValue)
                {
                    dateTimePicker.Value = date.Kind != DateTimeKind.Utc
                        ? DateUtil.ToIsoDate(TypeResolver.Resolve<IDateConverter>().ToServerTime(date))
                        : DateUtil.ToIsoDate(date);
                }
            }
            else
            {
                dateTimePicker.Value = value as string ?? string.Empty;
            }

            return dateTimePicker;
        }

        public bool CanReadValue(Control control)
        {
            return control is Sitecore.Web.UI.HtmlControls.DateTimePicker;
        }

        public void ReadValue(Control control, Hashtable result, VariableEditorContext context)
        {
            var controlValue = ((Sitecore.Web.UI.HtmlControls.Control)control).Value;
            result.Add("Value", DateUtil.IsoDateToDateTime(controlValue));
        }
    }
}
