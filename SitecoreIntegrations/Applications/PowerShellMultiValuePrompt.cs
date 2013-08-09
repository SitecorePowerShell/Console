using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Text;
using System.Web;
using System.Web.UI.WebControls;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Controls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Shell.Applications.ContentEditor;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Button = Sitecore.Web.UI.HtmlControls.Button;
using Checkbox = Sitecore.Web.UI.HtmlControls.Checkbox;
using DateTime = System.DateTime;
using Edit = Sitecore.Web.UI.HtmlControls.Edit;
using Label = Sitecore.Web.UI.HtmlControls.Label;
using Literal = Sitecore.Web.UI.HtmlControls.Literal;
using Panel = Sitecore.Web.UI.HtmlControls.Panel;

namespace Cognifide.PowerShell.SitecoreIntegrations.Applications
{
    public class PowerShellMultiValuePrompt : DialogPage
    {

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
                    Root = "/sitecore",
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

            var edit = new Edit();
            edit.Style.Add("float", "left");
            edit.ID = Control.GetUniqueID("variable_" + name + "_");
            edit.Class += " scContentControl textEdit";
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
                        else if (control is Edit)
                        {
                            result.Add("Value", (control as Edit).Value);
                        }

                    }
                }
            }
            return results.ToArray();
        }       

        protected void CancelClick()
        {
            SheerResponse.CloseWindow();
        }
    }
}