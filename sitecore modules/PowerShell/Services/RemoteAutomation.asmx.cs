using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Web.Services;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Resources.Media;
using Sitecore.Security.Authentication;
using Sitecore.Sites;
using Sitecore.Web;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    ///     Summary description for RemoteAutomation:
    ///     The service is used by to execute scripts blocks from remote locations
    ///     for the purpose of BDD tests and remote integration with Windows PowerShell.
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class RemoteAutomation : WebService
    {
        private void Login(string userName, string password)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }
                var loggedIn = AuthenticationManager.Login(userName, password, false);
                if (!loggedIn)
                {
                    throw new AuthenticationException("Unrecognized user or password mismatch.");
                }
            }
        }

        [WebMethod]
        public NameValue[] ExecuteScript(string userName, string password, string script, string returnVariables)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return new NameValue[0];
            }
            Login(userName, password);
            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                scriptSession.ExecuteScriptPart(scriptSession.Settings.Prescript);
                scriptSession.ExecuteScriptPart(script);

                var result = new List<NameValue>();

                if (scriptSession.Output.Count > 0)
                {
                    result.Add(new NameValue
                    {
                        Name = "output",
                        Value =
                            scriptSession.Output.Select(p => p.Terminated ? p.Text + "\n" : p.Text).Aggregate(
                                (current, next) => current + next)
                    });
                }
                result.AddRange(
                    returnVariables.Split('|').Select(variable => new NameValue
                    {
                        Name = variable,
                        Value = (scriptSession.GetVariable(variable) ?? string.Empty).ToString()
                    }));
                return result.ToArray();
            }
        }

        [WebMethod]
        public string ExecuteScriptBlock(string userName, string password, string script, string cliXmlArgs)
        {
            var requestUri = WebUtil.GetRequestUri();
            var site = SiteContextFactory.GetSiteContext(requestUri.Host, Sitecore.Context.Request.FilePath,
                requestUri.Port);
            return ExecuteScriptBlockinSite(userName, password, script, cliXmlArgs, site.Name);
        }

        [WebMethod]
        public string ExecuteScriptBlockinSite(string userName, string password, string script, string cliXmlArgs,
            string siteName)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return string.Empty;
            }
            Login(userName, password);

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                Sitecore.Context.SetActiveSite(siteName);
                scriptSession.SetVariable("cliXmlArgs", cliXmlArgs);
                scriptSession.ExecuteScriptPart(scriptSession.Settings.Prescript, false, true);
                scriptSession.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs", false, true);
                script = script.TrimEnd(' ', '\t', '\n');
                var outObjects = scriptSession.ExecuteScriptPart(script, false, false, false);
                scriptSession.SetVariable("results", outObjects);
                scriptSession.Output.Clear();
                scriptSession.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results");
                var result =
                    scriptSession.Output.Select(p => p.Terminated ? p.Text + "\n" : p.Text)
                        .Aggregate((current, next) => current + next);
                return result;
            }
        }

        [WebMethod]
        public bool UploadFile(string userName, string password, string filePath, byte[] fileContent, string database,
            string language)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return false;
            }

            try
            {
                Login(userName, password);

                var dirName = (Path.GetDirectoryName(filePath) ?? string.Empty).Replace('\\', '/');
                if (!dirName.StartsWith(Constants.MediaLibraryPath))
                {
                    dirName = Constants.MediaLibraryPath + (dirName.StartsWith("/") ? dirName : "/" + dirName);
                }

                var mco = new MediaCreatorOptions();
                mco.Database = Factory.GetDatabase(database);
                mco.Language = Language.Parse(language);
                mco.Versioned = Settings.Media.UploadAsVersionableByDefault;
                mco.Destination = string.Format("{0}/{1}", dirName, Path.GetFileNameWithoutExtension(filePath));

                var mc = new MediaCreator();
                using (var stream = new MemoryStream(fileContent))
                {
                    mc.CreateFromStream(stream, Path.GetFileName(filePath), mco);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error during uploading file using PowerShell web service", ex);
                return false;
            }
            return true;
        }

        [WebMethod]
        public byte[] DownloadFile(string userName, string password, string filePath, string database, string language)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return new byte[0];
            }

            try
            {
                Login(userName, password);

                var dirName = (Path.GetDirectoryName(filePath) ?? string.Empty).Replace('\\', '/');
                if (!dirName.StartsWith(Constants.MediaLibraryPath))
                {
                    dirName = Constants.MediaLibraryPath + (dirName.StartsWith("/") ? dirName : "/" + dirName);
                }
                var itemname = dirName + "/" + Path.GetFileNameWithoutExtension(filePath);
                var db = Factory.GetDatabase(database);
                var item = (MediaItem) db.GetItem(itemname);
                using (var stream = item.GetMediaStream())
                {
                    var result = new byte[stream.Length];
                    stream.Read(result, 0, (int) stream.Length);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error during uploading file using PowerShell web service", ex);
                return new byte[0];
            }
        }

        public class NameValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}