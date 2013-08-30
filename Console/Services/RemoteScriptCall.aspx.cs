using System;
using System.Net;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Data;
using Sitecore.Data.Items;
using AuthenticationManager = Sitecore.Security.Authentication.AuthenticationManager;

namespace Cognifide.PowerShell.Console.Services
{
    public partial class RemoteScriptCall : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            string userName = HttpContext.Current.Request.Params.Get("user");
            string password = HttpContext.Current.Request.Params.Get("password");
            string scriptParam = HttpContext.Current.Request.Params.Get("script");
            string scriptDbParam = HttpContext.Current.Request.Params.Get("scriptDb");
            string itemParam = HttpContext.Current.Request.Params.Get("item");
            string itemDbParam = HttpContext.Current.Request.Params.Get("itemDb");

            AuthenticationManager.Login(userName, password,false);

            Database scriptDb = Database.GetDatabase(scriptDbParam);
            Item scriptItem = scriptDb.GetItem(scriptParam);
            if (scriptItem == null)
            {
                scriptItem = scriptDb.GetItem("/sitecore/system/Modules/PowerShell/Script Library/" + scriptParam);
            }
            var session = new ScriptSession(ApplicationNames.Default);
            String script = (scriptItem.Fields[ScriptItemFieldNames.Script] != null)
                                ? scriptItem.Fields[ScriptItemFieldNames.Script].Value
                                : string.Empty;
            if (!string.IsNullOrEmpty(itemParam) && !string.IsNullOrEmpty(itemDbParam))
            {
                Database itemDb = Database.GetDatabase(itemDbParam);
                Item item = itemDb.GetItem(itemParam);
                if (item != null)
                session.SetItemLocationContext(item);
            }
            session.ExecuteScriptPart(script, true);
            Result.Text = session.Output.ToString();
            if (session.Output.HasErrors)
            {
                HttpContext.Current.Response.StatusCode = 424;
                HttpContext.Current.Response.StatusDescription = "Method Failure";
            }

        }
    }
}