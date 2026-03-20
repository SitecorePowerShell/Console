using System;
using System.Collections.Generic;
using System.Web;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.PreprocessRequest;
using Sitecore.Text;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Settings.Authorization;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Processors
{
    public class RewriteUrl : PreprocessRequestProcessor
    {
        private static readonly Dictionary<string, string> ApiVersionToServiceMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "script", WebServiceSettings.ServiceRemoting },
            { "2", WebServiceSettings.ServiceRestfulv2 },
            { "file", WebServiceSettings.ServiceFileDownload },
            { "media", WebServiceSettings.ServiceMediaDownload },
            { "handle", WebServiceSettings.ServiceHandleDownload },
        };

        public override void Process(PreprocessRequestArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            try
            {
                var url = TypeResolver.Resolve<IObsoletor>().GetRequestUrl(arguments);
                var localPath = url.LocalPath;

                if (!localPath.StartsWith("/-/script/", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var httpContext = HttpContext.Current;
                if (httpContext != null &&
                    string.Equals(httpContext.Request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
                {
                    HandleCorsPreflight(httpContext, localPath);
                    return;
                }

                if (localPath.StartsWith("/-/script/v1", StringComparison.OrdinalIgnoreCase))
                {
                    var sourceArray = url.LocalPath.TrimStart('/').Split('/');
                    if (sourceArray.Length < 3)
                    {
                        return;
                    }
                    var length = sourceArray.Length - 3;
                    var destinationArray = new string[length];
                    Array.Copy(sourceArray, 3, destinationArray, 0, length);
                    var scriptPath = $"/{string.Join("/", destinationArray)}";
                    var query = url.Query.TrimStart('?');
                    query += $"{(string.IsNullOrEmpty(query) ? "?" : "&")}script={scriptPath}&apiVersion=1";
                    WebUtil.RewriteUrl(
                        new UrlString
                        {
                            Path = "/sitecore modules/PowerShell/Services/RemoteScriptCall.ashx",
                            Query = query
                        }.ToString());
                }
                if (localPath.StartsWith("/-/script/v2", StringComparison.OrdinalIgnoreCase) ||
                    localPath.StartsWith("/-/script/media", StringComparison.OrdinalIgnoreCase) ||
                    localPath.StartsWith("/-/script/file", StringComparison.OrdinalIgnoreCase) ||
                    localPath.StartsWith("/-/script/handle", StringComparison.OrdinalIgnoreCase) ||
                    localPath.StartsWith("/-/script/script", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    var sourceArray = url.LocalPath.TrimStart('/').Split(new []{"/"}, StringSplitOptions.RemoveEmptyEntries);
                    if (sourceArray.Length < 3)
                    {
                        return;
                    }
                    if (sourceArray.Length == 3)
                    {
                        Array.Resize(ref sourceArray, 4);
                        sourceArray[3] = "";
                    }

                    var apiVersion = sourceArray[2].Is("v2") ? "2": sourceArray[2];
                    var length = sourceArray.Length - 4;
                    var destinationArray = new string[length];
                    var origin = sourceArray[3].ToLowerInvariant();
                    var database = apiVersion.Is("file") || apiVersion.Is("handle") ? string.Empty : origin;
                    Array.Copy(sourceArray, 4, destinationArray, 0, length);
                    var scriptPath = $"/{string.Join("/", destinationArray)}";
                    var query = url.Query.TrimStart('?');
                    query += $"{(string.IsNullOrEmpty(query) ? "" : "&")}script={scriptPath}&sc_database={database}&scriptDb={origin}&apiVersion={apiVersion}";
                    WebUtil.RewriteUrl(
                        new UrlString
                        {
                            Path = "/sitecore modules/PowerShell/Services/RemoteScriptCall.ashx",
                            Query = query
                        }.ToString());
                }
            }
            catch (Exception exception)
            {
                PowerShellLog.Error("Error during the SPE API call", exception);
            }
        }

        private static void HandleCorsPreflight(HttpContext httpContext, string localPath)
        {
            var sourceArray = localPath.TrimStart('/').Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries);
            if (sourceArray.Length < 3)
            {
                // End the response to prevent fallthrough to the handler.
                httpContext.Response.End();
                return;
            }

            var apiSegment = sourceArray[2];
            var apiVersion = apiSegment.Is("v1") ? "1" : apiSegment.Is("v2") ? "2" : apiSegment;

            if (!ApiVersionToServiceMapping.TryGetValue(apiVersion, out var serviceName))
            {
                // Unknown service -- end without CORS headers.
                httpContext.Response.End();
                return;
            }

            var cors = WebServiceSettings.GetCorsSettings(serviceName);
            if (cors == null)
            {
                // Service has no CORS config -- end without CORS headers.
                httpContext.Response.End();
                return;
            }

            var origin = httpContext.Request.Headers["Origin"];
            if (string.IsNullOrEmpty(origin) || !IsOriginAllowed(cors, origin))
            {
                // No Origin or disallowed origin -- end without CORS headers.
                httpContext.Response.End();
                return;
            }

            var response = httpContext.Response;
            response.StatusCode = 204;
            response.Headers["Access-Control-Allow-Origin"] = cors.AllowAnyOrigin ? "*" : origin;
            response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type, Content-Encoding";
            response.Headers["Access-Control-Max-Age"] = cors.MaxAge.ToString();

            if (cors.AllowCredentials)
            {
                response.Headers["Access-Control-Allow-Credentials"] = "true";
            }

            response.End();
        }

        private static bool IsOriginAllowed(WebServiceSettings.CorsSettings cors, string origin)
        {
            if (cors.AllowAnyOrigin)
            {
                return true;
            }

            return !string.IsNullOrEmpty(origin) && cors.AllowedOrigins != null && cors.AllowedOrigins.Contains(origin);
        }
    }
}