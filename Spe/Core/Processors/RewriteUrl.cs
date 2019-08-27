using System;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.PreprocessRequest;
using Sitecore.Text;
using Sitecore.Web;
using Spe.Abstractions.VersionDecoupling.Interfaces;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.VersionDecoupling;

namespace Spe.Core.Processors
{
    public class RewriteUrl : PreprocessRequestProcessor
    {
        public override void Process(PreprocessRequestArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            try
            {
                Assert.ArgumentNotNull(arguments.Context, "context");
                var url = TypeResolver.Resolve<IObsoletor>().GetRequestUrl(arguments);
                var localPath = url.LocalPath;

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
                    var sourceArray = url.LocalPath.TrimStart('/').Split('/');
                    if (sourceArray.Length < 4)
                    {
                        return;
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
    }
}