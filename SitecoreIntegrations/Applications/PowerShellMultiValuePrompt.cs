using System;
using System.Collections;
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
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using Button = Sitecore.Web.UI.HtmlControls.Button;
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
        protected GridPanel ValuePanel;

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string sid = WebUtil.GetQueryString("sid");
            object[] variables = (object[])HttpContext.Current.Session[sid];
            DialogHeader.Text = WebUtil.GetQueryString("te");
            DialogDescription.Text = WebUtil.GetQueryString("ds");

            AddControls(variables);
        }

        private void AddControls(object[] variables)
        {

            foreach (Hashtable variable in variables)
            {
                var label = new Label { Value = variable["Name"] + ":" };

                var input = GetVariableEditor(variable);
                label.For = input.ID;

                //i.Value = input.ID;
                //i.Controls.Add(input);
                ValuePanel.Controls.Add(label);
                ValuePanel.Controls.Add(input);
            }
        }

        private System.Web.UI.Control GetVariableEditor(Hashtable variable)
        {
            object value = variable["Value"];
            Type type = value.GetType();

            if (type == typeof (DateTime))
            {
                var dateTimePicker = new DateTimePicker();
                dateTimePicker.Style.Add("float", "left");
                dateTimePicker.ID = Control.GetUniqueID("input");
                dateTimePicker.ShowTime = false;
                dateTimePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.Display, "inline");
                dateTimePicker.Style.Add(System.Web.UI.HtmlTextWriterStyle.VerticalAlign, "middle");
                dateTimePicker.Value = DateUtil.GetShortIsoDateTime((DateTime) value);
                return dateTimePicker;
            }
            else
            {
                var edit = new Edit();
                edit.Style.Add("float", "left");
                edit.ID = Control.GetUniqueID("input");
                edit.Style.Add(System.Web.UI.HtmlTextWriterStyle.Display, "inline");
                edit.Style.Add(System.Web.UI.HtmlTextWriterStyle.VerticalAlign, "middle");
                edit.Value = value.ToString();
                return edit;                
            }
        }

        protected void OKClick()
        {
            string sid = WebUtil.GetQueryString("sid");
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }

        protected void CancelClick()
        {
            string sid = WebUtil.GetQueryString("sid");
            SheerResponse.SetDialogValue(sid);
            SheerResponse.CloseWindow();
        }
    }
}