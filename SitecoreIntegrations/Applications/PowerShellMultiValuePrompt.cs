using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Web;
using Sitecore;
using Sitecore.Controls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellMultiValuePrompt : DialogPage
    {

        private static Regex typeRegex = new Regex(@".*clr(?<type>[\w]+)\s*", RegexOptions.Singleline | RegexOptions.Compiled);
        protected Literal Result;
        protected Literal DialogHeader;
        protected Literal DialogDescription;
        protected Border DataContextPanel;
        protected GridPanel ValuePanel;
        protected Button OKButton;
        protected Button CancelButton;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Sitecore.Context.ClientPage.IsEvent)
                return;
            string sid = WebUtil.GetQueryString("sid");
            var variables = (object[]) HttpContext.Current.Session[sid];

            DialogHeader.Text = WebUtil.GetQueryString("te");
            DialogDescription.Text = WebUtil.GetQueryString("ds");
            
            string okText = WebUtil.GetQueryString("ob");
            if (!string.IsNullOrEmpty(okText))
            {
                OKButton.Header = okText;
            }
            
            string cancelText = WebUtil.GetQueryString("cb");
            if (!string.IsNullOrEmpty(cancelText))
            {
                CancelButton.Header = cancelText;
            }

            AddControls(variables);
        }

        private void AddControls(object[] variables)
        {

            foreach (Hashtable variable in variables)
            {
                var name = variable["Title"] as string;
                if (variable["Value"].GetType() != typeof (bool))
                {
                    var label = new Literal {Text = name + ":"};
                    label.Class = "varTitle";
                    ValuePanel.Controls.Add(label);
                }
                var input = GetVariableEditor(variable);
                ValuePanel.Controls.Add(input);
            }
        }

        private System.Web.UI.Control GetVariableEditor(Hashtable variable)
        {
            object value = variable["Value"];
            string name = (string) variable["Name"];
            Type type = value.GetType();

            if (type == typeof (DateTime))
            {
                var dateTimePicker = new DateTimePicker
                {
                    ID = Control.GetUniqueID("variable_" + name + "_"),
                    ShowTime = variable["ShowTime"] != null && (bool) variable["ShowTime"],
                    Value = DateUtil.ToIsoDate((DateTime) value)
                };
                return dateTimePicker;
            }

            if (type == typeof (Item))
            {
                var item = (Item) value;
                var dataContext = new DataContext
                {
                    DefaultItem = item.Paths.Path,
                    ID = Control.GetUniqueID("dataContext"),
                    DataViewName = "Master",
                    Root = variable["Root"] as string ?? "/sitecore",
                    Parameters = "databasename=" + item.Database.Name,
                    Database = item.Database.Name,
                    Selected = new[] {new DataUri(item.ID, item.Language, item.Version)},
                    Folder = item.ID.ToString(),
                    Language = item.Language,
                    Version = item.Version
                };
                DataContextPanel.Controls.Add(dataContext);

                var treePicker = new TreePicker
                {
                    ID = Control.GetUniqueID("variable_" + name + "_"),
                    Value = item.ID.ToString(),
                    DataContext = dataContext.ID
                };
                treePicker.Class += " treePicker";
                return treePicker;
            }

            if (type == typeof(bool))
            {
                var checkBox = new Checkbox
                {
                    ID = Control.GetUniqueID("variable_" + name + "_"),
                    Header = (string) variable["Title"],
                    HeaderStyle = "margin-top:20px; display:inline-block;",
                    Checked = (bool) value
                };
                checkBox.Class = "varCheckbox";
                return checkBox;
            }

            Control edit = new Edit();
            if (variable["lines"] != null && ((int)variable["lines"] > 1))
            {
                edit = new Memo();
                edit.Attributes.Add("rows", variable["lines"].ToString());
            }
            else if (variable["Options"] != null)
            {
                edit = new Combobox();
                string[] options = ((string) variable["Options"]).Split('|');
                int i = 0;
                while (i < options.Length)
                {
                    var item = new ListItem()
                    {
                        Header = options[i++],
                        Value = options[i++],
                    };
                    edit.Controls.Add(item);
                }
            }
            else
            {
                edit = new Edit();
            }
            if (!string.IsNullOrEmpty((string)variable["Tooltip"]))
            {
                edit.ToolTip = (string)variable["Tooltip"];
            }
            edit.Style.Add("float", "left");
            edit.ID = Control.GetUniqueID("variable_" + name + "_");
            edit.Class += " scContentControl textEdit clr"+value.GetType().Name;
            edit.Value = value.ToString();
            return edit;
        }

        protected void OKClick()
        {
            var variables = GetVariableValues();
            string sid = WebUtil.GetQueryString("sid");
            HttpContext.Current.Session[sid] = variables;
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }

        private object[] GetVariableValues()
        {
            var results = new List<object>();
            foreach (Control control in ValuePanel.Controls)
            {
                string controlId = control.ID;
                if (controlId != null && controlId.StartsWith("variable_"))
                {
                    string[] parts = controlId.Split('_');
                    
                    var result = new Hashtable(2);
                    results.Add(result);
                    result.Add("Name",parts[1]);

                    var controlValue = control.Value;
                    if (controlValue != null)
                    {
                        if (control is DateTimePicker)
                        {
                            result.Add("Value", DateUtil.IsoDateToDateTime(controlValue));
                        }
                        else if (control is TreePicker)
                        {
                            string contextID = (control as TreePicker).DataContext;
                            var context = (DataContext) DataContextPanel.FindControl(contextID);
                            result.Add("Value", context.CurrentItem);
                        }
                        else if (control is Checkbox)
                        {
                            bool boolValue = (control as Checkbox).Checked;
                            result.Add("Value", boolValue);
                        }
                        else if (control is Edit || control is Memo)
                        {
                            string value = control.Value;
                            string type = GetClrTypeName(control.Class);
                            switch (type)
                            {
                                case("Int16"):
                                    result.Add("Value", Int16.Parse(value));
                                    break;
                                case ("Int32"):
                                    result.Add("Value", Int32.Parse(value));
                                    break;
                                case ("Int64"):
                                    result.Add("Value", Int64.Parse(value));
                                    break;
                                case ("UInt16"):
                                    result.Add("Value", UInt16.Parse(value));
                                    break;
                                case ("UInt32"):
                                    result.Add("Value", UInt32.Parse(value));
                                    break;
                                case ("UInt64"):
                                    result.Add("Value", UInt64.Parse(value));
                                    break;
                                case ("Byte"):
                                    result.Add("Value", Byte.Parse(value));
                                    break;
                                case ("Single"):
                                    result.Add("Value", Single.Parse(value));
                                    break;
                                case ("Double"):
                                    result.Add("Value", Double.Parse(value));
                                    break;
                                case ("Decimal"):
                                    result.Add("Value", Decimal.Parse(value));
                                    break;
                                default:
                                    result.Add("Value", value);
                                    break;
                            }
                        }

                    }
                }
            }
            return results.ToArray();
        }

        protected string GetClrTypeName(string classNames)
        {
            Match typeMatch = typeRegex.Match(classNames);
            if (typeMatch.Success)
            {
                return typeMatch.Groups["type"].Value;
            }
            return string.Empty;
        }

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }
    }
}