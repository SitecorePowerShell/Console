using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Web;
using System.Web.SessionState;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.IO;
using Sitecore.Resources.Media;
using Sitecore.Security.Authentication;
using Sitecore.Web;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    ///     The handler allows for execution of scripts stored within Script Library
    ///     it also allows those scripts to be parametrized.
    /// </summary>
    public class RemoteScriptCall : IHttpHandler, IRequiresSessionState
    {
        private SortedDictionary<string, SortedDictionary<string, ApiScript>> apiScripts;

        public RemoteScriptCall()
        {
            ModuleManager.OnInvalidate += InvalidateCache;
            apiScripts = null;
        }

        public void ProcessRequest(HttpContext context)
        {
            var request = HttpContext.Current.Request;
            var userName = request.Params.Get("user");
            if (!string.IsNullOrEmpty(userName) && !userName.Contains("\\"))
            {
                userName = "sitecore\\" + userName;
            }
            var password = request.Params.Get("password");
            var itemParam = request.Params.Get("script");
            var pathParam = request.Params.Get("path");
            var originParam = request.Params.Get("scriptDb");
            var apiVersion = request.Params.Get("apiVersion");

            switch (apiVersion)
            {
                case "1":
                    if (!WebServiceSettings.ServiceEnabledRestfulv1)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                case "2":
                    if (!WebServiceSettings.ServiceEnabledRestfulv2)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                case "file":
                    if (!WebServiceSettings.ServiceEnabledFileDownload)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                case "media":
                    if (!WebServiceSettings.ServiceEnabledMediaDownload)
                    {
                        HttpContext.Current.Response.StatusCode = 403;
                        return;
                    }
                    break;
                default:
                    HttpContext.Current.Response.StatusCode = 403;
                    return;
            }

            if(!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                AuthenticationManager.Login(userName, password, false);
            }

            var authenticated = Context.IsLoggedIn;
            var scriptDb =
                apiVersion.Is("file") || !authenticated ||
                string.IsNullOrEmpty(originParam) || originParam == "current"
                    ? Context.Database
                    : Database.GetDatabase(originParam);
            var dbName = scriptDb.Name;

            Item scriptItem;

            switch (apiVersion)
            {
                case "1":
                    scriptItem = scriptDb.GetItem(itemParam) ??
                                 scriptDb.GetItem(ApplicationSettings.ScriptLibraryPath + itemParam);
                    break;
                case "media":
                    itemParam = itemParam.Trim('/', '\\');
                    var mediaItem = (MediaItem) scriptDb.GetItem(itemParam) ??
                                    scriptDb.GetItem(ApplicationSettings.MediaLibraryPath + itemParam);
                    Stream mediaStream = mediaItem.GetMediaStream();
                    string str = mediaItem.Extension;
                    if (!str.StartsWith(".", StringComparison.InvariantCulture))
                        str = "." + str;
                    WriteCacheHeaders(mediaItem.Name + str, mediaStream.Length);
                    WebUtil.TransmitStream(mediaStream, HttpContext.Current.Response, Settings.Media.StreamBufferSize);
                    return;
                case "file":
                    var file = string.Empty;
                    switch (originParam)
                    {
                        case ("data"):
                            file = Settings.DataFolder;
                            break;
                        case ("logs"):
                            file = Settings.LogFolder;
                            break;
                        case ("media"):
                            file = Settings.MediaFolder;
                            break;
                        case ("package"):
                            file = Settings.PackagePath;
                            break;
                        case ("serialization"):
                            file = Settings.SerializationFolder;
                            break;
                        case ("apptemp"):
                            file = Settings.TempFolderPath;
                            break;
                        case ("debug"):
                            file = Settings.DebugFolder;
                            break;
                        case ("index"):
                            file = Settings.IndexFolder;
                            break;
                        case ("layout"):
                            file = Settings.LayoutFolder;
                            break;
                        case ("app"):
                            file = HttpRuntime.AppDomainAppPath;
                            break;
                        case ("temp"):
                            file = Environment.GetEnvironmentVariable("temp");
                            break;
                        case ("tmp"):
                            file = Environment.GetEnvironmentVariable("tmp");
                            break;
                        default:
                            file = originParam + ":\\";
                            break;
                    }

                    if (file == null)
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                    }
                    else
                    {
                        file = Path.Combine(file, pathParam);
                        file = FileUtil.MapPath(file);
                        if (!File.Exists(file))
                        {
                            HttpContext.Current.Response.StatusCode = 404;
                            return;
                        }
                        FileInfo fileInfo = new FileInfo(file);
                        WriteCacheHeaders(Path.GetFileName(file), fileInfo.Length);
                        HttpContext.Current.Response.TransmitFile(file);
                    }
                    return;
                default:
                    UpdateCache(dbName);
                    if (!apiScripts.ContainsKey(dbName))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    var dbScripts = apiScripts[scriptDb.Name];
                    if (!dbScripts.ContainsKey(itemParam))
                    {
                        HttpContext.Current.Response.StatusCode = 404;
                        return;
                    }
                    scriptItem = scriptDb.GetItem(dbScripts[itemParam].Id);
                    break;
            }

            if (scriptItem == null || scriptItem.Fields[ScriptItemFieldNames.Script] == null)
            {
                HttpContext.Current.Response.StatusCode = 404;
                return;
            }

            using (var session = ScriptSessionManager.NewSession(ApplicationNames.Default, true))
            {
                var script = scriptItem.Fields[ScriptItemFieldNames.Script].Value;

                if (Context.Database != null)
                {
                    var item = Context.Database.GetRootItem();
                    if (item != null)
                        session.SetItemLocationContext(item);
                }
                session.SetExecutedScript(scriptItem);

                context.Response.ContentType = "text/plain";

                var scriptArguments = new Hashtable();

                foreach (var param in HttpContext.Current.Request.QueryString.AllKeys)
                {
                    var paramValue = HttpContext.Current.Request.QueryString[param];
                    if (string.IsNullOrEmpty(param)) continue;
                    if (string.IsNullOrEmpty(paramValue)) continue;

                    scriptArguments[param] = paramValue;
                }

                foreach (var param in HttpContext.Current.Request.Params.AllKeys)
                {
                    var paramValue = HttpContext.Current.Request.Params[param];
                    if (string.IsNullOrEmpty(param)) continue;
                    if (string.IsNullOrEmpty(paramValue)) continue;

                    if (session.GetVariable(param) == null)
                    {
                        session.SetVariable(param, paramValue);
                    }
                }

                session.SetVariable("scriptArguments", scriptArguments);

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
            get { return true; }
        }

        public void InvalidateCache(object sender, EventArgs e)
        {
            apiScripts = null;
        }

        private void UpdateCache(string dbName)
        {
            if (apiScripts == null)
            {
                apiScripts = new SortedDictionary<string, SortedDictionary<string, ApiScript>>(StringComparer.OrdinalIgnoreCase);
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi);
                BuildCache(roots);
            }

            if (!apiScripts.ContainsKey(dbName))
            {
                apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.WebApi, dbName);
                BuildCache(roots);
            }
        }

        private void BuildCache(List<Item> roots)
        {
            foreach (var root in roots)
            {
                var path = PathUtilities.PreparePathForQuery(root.Paths.Path);
                var rootPath = root.Paths.Path;
                var query = string.Format(
                    "{0}//*[@@TemplateId=\"{{DD22F1B3-BD87-4DB2-9E7D-F7A496888D43}}\"]",
                    path);
                try
                {
                    var results = root.Database.SelectItems(query);
                    foreach (var result in results)
                    {
                        var scriptPath = result.Paths.Path.Substring(rootPath.Length);
                        var dbName = result.Database.Name;
                        if (!apiScripts.ContainsKey(dbName))
                        {
                            apiScripts.Add(dbName, new SortedDictionary<string, ApiScript>(StringComparer.OrdinalIgnoreCase));
                        }
                        apiScripts[dbName].Add(scriptPath, new ApiScript
                        {
                            Database = result.Database.Name,
                            Id = result.ID,
                            Path = scriptPath
                        });
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Error while querying for items", ex);
                }
            }
        }

        private void WriteCacheHeaders(string filename, long length)
        {
            Assert.ArgumentNotNull((object)filename, "filename");
            var response = HttpContext.Current.Response;
            response.ClearHeaders();
            response.AddHeader("Content-Type", "application/octet-stream");
            response.AddHeader("Content-Disposition", "attachment; filename=\"" + filename + "\"");
            response.AddHeader("Content-Length", length.ToString());
            response.AddHeader("Content-Transfer-Encoding", "binary");
            response.CacheControl = "private";
        }
        public class ApiScript
        {
            public string Path { get; set; }
            public string Database { get; set; }
            public ID Id { get; set; }
        }
    }
}