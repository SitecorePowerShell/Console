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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (Sitecore.Context.ClientPage.IsEvent)
                return;
            string sid = WebUtil.GetQueryString("sid");
            object[] variables = (object[]) HttpContext.Current.Session[sid];
            DialogHeader.Text = WebUtil.GetQueryString("te");
            DialogDescription.Text = WebUtil.GetQueryString("ds");
            AddControls(variables);
        }

        private void AddControls(object[] variables)
        {

            foreach (Hashtable variable in variables)
            {
                var name = variable["Title"] as string;
                var label = new Literal { Text = name + ":" };
                var input = GetVariableEditor(variable);
                ValuePanel.Controls.Add(label);
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
                var dateTimePicker = new DateTimePicker();
                dateTimePicker.Style.Add("float", "left");
                dateTimePicker.ID = Control.GetUniqueID("variable_" + name + "_");
                dateTimePicker.ShowTime = variable["ShowTime"] != null && (bool) variable["ShowTime"];
                dateTimePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.Display, "inline");
                dateTimePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.VerticalAlign, "middle");
                dateTimePicker.Value = DateUtil.ToIsoDate((DateTime) value);
                return dateTimePicker;
            }

            if (type == typeof (Item))
            {
                var item = (Item) value;
                var dataContext = new DataContext();
                dataContext.DefaultItem = item.Paths.Path;
                dataContext.ID = Control.GetUniqueID("dataContext");
                dataContext.DataViewName = "Master";
                dataContext.Root = "/sitecore";
                dataContext.AddSelected(new DataUri(item.ID, item.Language, item.Version));
                DataContextPanel.Controls.Add(dataContext);
                dataContext.Parameters = "databasename=" + item.Database.Name;

                var treePicker = new TreePicker();
                treePicker.Style.Add("float", "left");
                treePicker.ID = Control.GetUniqueID("variable_" + name + "_");
                treePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.Display, "inline");
                treePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.VerticalAlign, "middle");
                treePicker.Class += " treePicker";
                treePicker.Value = item.Paths.Path;
                treePicker.DataContext = dataContext.ID;
                return treePicker;
            }

            var edit = new Edit();
            edit.Style.Add("float", "left");
            edit.ID = Control.GetUniqueID("variable_" + name + "_");
            edit.Style.Add(System.Web.UI.HtmlTextWriterStyle.Display, "inline");
            edit.Style.Add(System.Web.UI.HtmlTextWriterStyle.VerticalAlign, "middle");
            edit.Style.Add(System.Web.UI.HtmlTextWriterStyle.Width, "300px");
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
                            result.Add("Value", (control as TreePicker).GetContextItem());
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
            string sid = WebUtil.GetQueryString("sid");
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }
    }
}