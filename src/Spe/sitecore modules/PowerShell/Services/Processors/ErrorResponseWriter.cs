using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using Spe.Core.Extensions;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    internal static class ErrorResponseWriter
    {
        public static string JsonEscape(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var sb = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                switch (c)
                {
                    case '\\': sb.Append("\\\\"); break;
                    case '"': sb.Append("\\\""); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.AppendFormat(CultureInfo.InvariantCulture, "\\u{0:x4}", (int)c);
                        else sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        public static void SetErrorResponse(HttpContext context, int statusCode, string message, bool suppressFormsAuth = false)
        {
            context.Response.StatusCode = statusCode;
            context.Response.StatusDescription = message;

            var outputFormat = context.Request.QueryString["outputFormat"];
            if (!string.IsNullOrEmpty(outputFormat) && outputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.ContentType = "application/json";
                var useStructured = context.Request.QueryString["errorFormat"].Is("structured");
                if (useStructured)
                {
                    var errorCategory = statusCode == 401 || statusCode == 403 ? "SecurityError" : "ConnectionError";
                    var rid = RemotingRequestContext.GetRequestId();
                    var errorEntry = new Hashtable
                    {
                        ["correlationId"] = rid,
                        ["errorCategory"] = errorCategory,
                        ["fullyQualifiedErrorId"] = $"Spe.Remoting.Http{statusCode}",
                        ["message"] = $"Remoting request rejected with HTTP {statusCode}. See SPE log id={rid}."
                    };
                    if (WebServiceSettings.DetailedErrors)
                    {
                        // Verbose form keeps the SPE-controlled message and the
                        // pseudo exceptionType for backward compat with callers
                        // that read those fields. The message itself is whatever
                        // the caller of SetErrorResponse passed in - typically
                        // SPE-internal text but in some sites (e.g. policy block)
                        // it carries policy and command names.
                        errorEntry["message"] = message;
                        errorEntry["exceptionType"] = "System.Web.HttpException";
                        errorEntry["exceptionMessage"] = message;
                    }
                    var errorBody = new Hashtable
                    {
                        ["output"] = new object[0],
                        ["errors"] = new[] { errorEntry }
                    };
                    context.Response.Write(new JavaScriptSerializer().Serialize(errorBody));
                }
                else
                {
                    var errorBody = new Hashtable
                    {
                        ["output"] = new object[0],
                        ["errors"] = new[] { message }
                    };
                    context.Response.Write(new JavaScriptSerializer().Serialize(errorBody));
                }
            }

            if (suppressFormsAuth)
            {
                context.Response.SuppressFormsAuthenticationRedirect = true;
                context.Response.TrySkipIisCustomErrors = true;
                if (context.Response.ContentType != "application/json")
                {
                    context.Response.ContentType = "text/plain";
                }
            }
        }
    }
}
