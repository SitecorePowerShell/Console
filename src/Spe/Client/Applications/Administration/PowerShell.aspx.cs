using Sitecore.Diagnostics;
using System;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Sitecore.Data.Items;
using Spe.Core.Host;
using Spe.Core.Settings;

namespace Spe.Client.Applications.Administration
{
    public class PowerShell : NonSecurePage
    {
        protected HtmlForm Form1;

        protected PlaceHolder ErrorMessage;

        protected TextBox Query;

        protected Button Button1;

        protected HtmlGenericControl Output;

        protected void Execute(object sender, EventArgs e)
        {
            var text = this.Query.Text;
            if (string.IsNullOrEmpty(text))
            {
                this.ShowError("Query is empty");
                return;
            }
            try
            {
                using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, false))
                {
                    session.ExecuteScriptPart(text, true);
                    Output.InnerHtml = $"<pre ID='ScriptResultCode'>{session.Output.ToHtml()}</pre>";
                }
            }
            catch (Exception exception1)
            {
                Exception exception = exception1;
                this.ShowError(exception.Message);
                Log.Error("An error occured when using PowerShell page.", exception, this);
            }
        }

        protected override void OnInit(EventArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            base.CheckSecurity(true);
            base.OnInit(arguments);
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!base.IsPostBack)
            {

            }
        }

        protected virtual void ShowError(string text)
        {
            this.ErrorMessage.Controls.Clear();
            this.ErrorMessage.Controls.Add(new LiteralControl(string.Concat("<p class='error'><span>", text, "</span></p>")));
        }
    }
}