using System.Web;
using System.Web.Script.Serialization;
using Sitecore.Configuration;
using Sitecore.Data;
using Spe.Core.Extensions;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class ItemSourceProcessor
    {
        public static void ProcessItemSource(HttpContext context, string dbName, string idParam)
        {
            var db = string.IsNullOrEmpty(dbName) ? null : Factory.GetDatabase(dbName, false);
            // GetItem runs as the signed-in user, so an item they cannot read is a 404 like a missing one
            var item = db != null && ID.TryParse(idParam, out var id) ? db.GetItem(id) : null;
            if (item == null)
            {
                context.Response.StatusCode = 404;
                return;
            }

            var source = item.GetItemSource();
            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.Write(new JavaScriptSerializer().Serialize(new
            {
                source,
                resource = source == "Resource" || source == "Overridden",
                sql = source == "Database" || source == "Overridden"
            }));
        }
    }
}
