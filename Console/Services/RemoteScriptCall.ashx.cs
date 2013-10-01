using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    /// Summary description for Handler1
    /// </summary>
    public class RemoteScriptCall : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string userName = HttpContext.Current.Request.Params.Get("user");
            string password = HttpContext.Current.Request.Params.Get("password");
            string scriptParam = HttpContext.Current.Request.Params.Get("script");
            string scriptDbParam = HttpContext.Current.Request.Params.Get("scriptDb");
            string itemParam = HttpContext.Current.Request.Params.Get("item");
            string itemDbParam = HttpContext.Current.Request.Params.Get("itemDb");

            bool authenticated = !string.IsNullOrEmpty(userName) &&
                                 !string.IsNullOrEmpty(password) &&
                                 AuthenticationManager.Login(userName, password, false);

            Database scriptDb = !authenticated || string.IsNullOrEmpty(scriptDbParam)
                ? Sitecore.Context.Database
                : Database.GetDatabase(scriptDbParam);

            Database itemDb = !authenticated || string.IsNullOrEmpty(itemDbParam)
                ? Sitecore.Context.Database
                : Database.GetDatabase(itemDbParam);

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

                if (!string.IsNullOrEmpty(itemParam))
                {
                    Item item = itemDb.GetItem(itemParam);
                    if (item != null)
                        session.SetItemLocationContext(item);
                }

                context.Response.ContentType = "text/plain";

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
            get
            {
                return false;
            }
        }
    }
}