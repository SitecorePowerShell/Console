using System;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    /// The handler allows for execution of scripts stored within Script Library
    /// it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            string userName = HttpContext.Current.Request.Params.Get("user");
            string password = HttpContext.Current.Request.Params.Get("password");
            string scriptParam = HttpContext.Current.Request.Params.Get("script");
            string scriptDbParam = HttpContext.Current.Request.Params.Get("scriptDb");

            bool authenticated = !string.IsNullOrEmpty(userName) &&
                                 !string.IsNullOrEmpty(password) &&
                                 AuthenticationManager.Login(userName, password, false);

            Database scriptDb = !authenticated || string.IsNullOrEmpty(scriptDbParam)
                ? Context.Database
                : Database.GetDatabase(scriptDbParam);

            Item scriptItem = scriptDb.GetItem(scriptParam);

            if (scriptItem == null)
            {
                scriptItem = scriptDb.GetItem(ScriptLibrary.Path + scriptParam);
            }

            if (scriptItem == null || scriptItem.Fields[ScriptItemFieldNames.Script] == null)
            {
                return;
            }

            using (var session = new ScriptSession(ApplicationNames.Default))
            {
                String script = scriptItem.Fields[ScriptItemFieldNames.Script].Value;

                Item item = Context.Database.GetRootItem();
                if (item != null)
                    session.SetItemLocationContext(item);

                session.SetExecutedScript(scriptItem);

                context.Response.ContentType = "text/plain";

                foreach (var param in HttpContext.Current.Request.Params.AllKeys)
                {
                    session.SetVariable(param, HttpContext.Current.Request.Params[param]);
                }

                session.ExecuteScriptPart(script, true);

                context.Response.Write(session.Output.ToString());

                if (session.Output.HasErrors)
                {
                    context.Response.StatusCode = 424;
                    context.Response.StatusDescription = "Method Failure";
                }
            }
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}