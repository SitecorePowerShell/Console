using System;
using System.Linq;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.PreprocessRequest;
using Sitecore.Text;
using Sitecore.Web;

namespace Cognifide.PowerShell.Core.Processors
{
    public class RewriteUrl : PreprocessRequestProcessor
    {
        public override void Process(PreprocessRequestArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            try
            {
                Assert.ArgumentNotNull(arguments.Context, "context");
                var url = arguments.Context.Request.Url;
                var localPath = url.LocalPath;

                //Compatibility with 2.x services location now removed - uncomment the following to restore
                // Issue #511
                /*  
                if (localPath.StartsWith("/Console/", StringComparison.OrdinalIgnoreCase))
                  {
                      // this bit is for compatibility of solutions integrating with SPE 2.x services in mind
                      WebUtil.RewriteUrl(
                          new UrlString
                          {
                              Path = localPath.ToLowerInvariant().Replace("/console/", "/sitecore modules/PowerShell/"),
                              Query = url.Query
                          }.ToString());
                  }
                */
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
                    var scriptPath = string.Format("/{0}", string.Join("/", destinationArray));
                    var query = url.Query.TrimStart('?');
                    query += string.Format("{0}script={1}&apiVersion=1", string.IsNullOrEmpty(query) ? "?" : "&",
                        scriptPath);
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
                    localPath.StartsWith("/-/script/handle", StringComparison.OrdinalIgnoreCase)
                    )
                {
                    var sourceArray = url.LocalPath.TrimStart('/').Split('/');
                    if (sourceArray.Length < 4)
                    {
                        return;
                    }
                    string apiVersion = sourceArray[2].Is("v2") ? "2": sourceArray[2];
                    var length = sourceArray.Length - 4;
                    var destinationArray = new string[length];
                    var origin = sourceArray[3].ToLowerInvariant();
                    string database = apiVersion.Is("file") || apiVersion.Is("handle") ? string.Empty : origin;
                    Array.Copy(sourceArray, 4, destinationArray, 0, length);
                    var scriptPath = string.Format("/{0}", string.Join("/", destinationArray));
                    var query = url.Query.TrimStart('?');
                    query += string.Format("{0}script={1}&sc_database={2}&scriptDb={3}&apiVersion={4}",
                        string.IsNullOrEmpty(query) ? "" : "&", scriptPath, database, origin, apiVersion);
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