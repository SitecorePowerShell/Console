using System;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.PreprocessRequest;
using Sitecore.Text;
using Sitecore.Web;

namespace Cognifide.PowerShell.SitecoreIntegrations.Processors
{
    public class RewriteUrl : PreprocessRequestProcessor
    {
        private static int GetVersion(string path)
        {
            int num;
            Assert.ArgumentNotNull(path, "path");
            string str = path.TrimStart(new[] {'/'}).Split(new[] {'/'})[2];
            Assert.IsTrue(str.StartsWith("v"), "Version token is wrong.");
            Assert.IsTrue(int.TryParse(str.Replace("v", string.Empty), out num), "Version not recognized.");
            return num;
        }

        public override void Process(PreprocessRequestArgs arguments)
        {
            Assert.ArgumentNotNull(arguments, "arguments");
            try
            {
                string localPath = arguments.Context.Request.Url.LocalPath;
                if (localPath.StartsWith("/-/script/v1"))
                {
                    Assert.ArgumentNotNull(arguments.Context, "context");
                    Uri url = arguments.Context.Request.Url;
                    string[] sourceArray = url.LocalPath.TrimStart('/').Split('/');
                    if (sourceArray.Length < 3)
                    {
                        return;
                    }
                    int length = sourceArray.Length - 3;
                    var destinationArray = new string[length];
                    Array.Copy(sourceArray, 3, destinationArray, 0, length);
                    string scriptPath = string.Format("/{0}", string.Join("/", destinationArray));
                    string query = url.Query.TrimStart('?');
                    query += string.Format("{0}script={1}", string.IsNullOrEmpty(query) ? "?" : "&", scriptPath);
                    WebUtil.RewriteUrl(
                        new UrlString {Path = "/Console/Services/RemoteScriptCall.ashx", Query = query}.ToString());
                }
                if (localPath.StartsWith("/-/script/v2"))
                {
                    Assert.ArgumentNotNull(arguments.Context, "context");
                    Uri url = arguments.Context.Request.Url;
                    string[] sourceArray = url.LocalPath.TrimStart('/').Split('/');
                    if (sourceArray.Length < 4)
                    {
                        return;
                    }
                    int length = sourceArray.Length - 4;
                    var destinationArray = new string[length];
                    Array.Copy(sourceArray, 4, destinationArray, 0, length);
                    string scriptPath = string.Format("/{0}", string.Join("/", destinationArray));
                    string query = url.Query.TrimStart('?');
                    query += string.Format("{0}script={1}&sc_database={2}", string.IsNullOrEmpty(query) ? "?" : "&", scriptPath, sourceArray[3]);
                    WebUtil.RewriteUrl(
                        new UrlString { Path = "/Console/Services/RemoteScriptCall.ashx", Query = query }.ToString());
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error during the SPE API call", exception);
            }
        }
    }
}