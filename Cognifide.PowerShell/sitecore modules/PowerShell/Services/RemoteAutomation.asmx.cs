using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Web.Services;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.Utility;
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
    [WebService(Namespace = "http://sitecorepowershellextensions/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class RemoteAutomation : WebService
    {
        private bool Login(string userName, string password)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                PowerShellLog.Info($"User '{userName}' calling the Remoting Automation service.");

                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }

                if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceRemoting,userName,false))
                {
                    PowerShellLog.Error($"User `{userName}` tried to access the service but was not permitted to do so.");
                    return false;
                }

                var loggedIn = AuthenticationManager.Login(userName, password, false);
                if (!loggedIn)
                {
                    PowerShellLog.Error($"User '{userName}' was not recognized or provided wrong password.");
                }
                else
                {
                    PowerShellLog.Info($"User '{userName}' successfully logged in to the Remoting Automation service.");
                }
                return loggedIn;
            }
            PowerShellLog.Info($"Unsuccessfuly login with empty username or password. Username: '{userName}'.");
            return false;
        }

        [WebMethod]
        public NameValue[] ExecuteScript(string userName, string password, string script, string returnVariables)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return new NameValue[0];
            }

            if (!Login(userName, password))
            {
                return new[]
                {
                    new NameValue() { Name = "login failed", Value = "login failed" } 
                };
            }

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
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
            return ExecuteScriptBlock2(userName, password, script, cliXmlArgs, null);
        }

        [WebMethod]
        public string ExecuteScriptBlock2(string userName, string password, string script, string cliXmlArgs,
            string sessionId)
        {
            var requestUri = WebUtil.GetRequestUri();
            var site = SiteContextFactory.GetSiteContext(requestUri.Host, Sitecore.Context.Request.FilePath,
                requestUri.Port);
            return ExecuteScriptBlockinSite2(userName, password, script, cliXmlArgs, site.Name, sessionId);
        }

        [WebMethod]
        public string ExecuteScriptBlockinSite(string userName, string password, string script, string cliXmlArgs,
            string siteName)
        {
            return ExecuteScriptBlockinSite2(userName, password, script, cliXmlArgs, siteName, null);
        }

        [WebMethod]
        public string DisposeScriptSession(string userName, string password, string sessionId)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return string.Empty;
            }

            if (!Login(userName, password))
            {
                return "login failed";
            }

            if (ScriptSessionManager.SessionExists(sessionId))
            {
                var session = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);
                ScriptSessionManager.RemoveSession(session);
                return "removed";
            }

            return "not found";
        }

        [WebMethod]
        public string ExecuteScriptBlockinSite2(string userName, string password, string script, string cliXmlArgs,
            string siteName, string sessionId)
        {
            if (!WebServiceSettings.ServiceEnabledRemoting)
            {
                return string.Empty;
            }
            if (!Login(userName, password))
            {
                return "<Objs xmlns=\"http://schemas.microsoft.com/powershell/2004/04\"><Obj RefId=\"0\"><S>login failed</S></Obj></Objs>";
            }

            var scriptSession = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            Sitecore.Context.SetActiveSite(siteName);

            if (!String.IsNullOrEmpty(cliXmlArgs))
            {
                scriptSession.SetVariable("cliXmlArgs", cliXmlArgs);
                scriptSession.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs", false, true);
                script = script.TrimEnd(' ', '\t', '\n');
            }
            var outObjects = scriptSession.ExecuteScriptPart(script, false, false, false);
            if (scriptSession.LastErrors != null && scriptSession.LastErrors.Any())
            {
                outObjects.AddRange(scriptSession.LastErrors);
            }
            scriptSession.SetVariable("results", outObjects);
            scriptSession.Output.Clear();
            scriptSession.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results");
            var result = scriptSession.Output.Select(p => p.Text).Aggregate((current, next) => current + next);

            if (String.IsNullOrEmpty(sessionId))
            {
                ScriptSessionManager.RemoveSession(scriptSession);
            }
            return result;
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
                if (!Login(userName, password))
                {
                    return false;
                }

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
                PowerShellLog.Error("Error during uploading file using PowerShell web service", ex);
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
                if (!Login(userName, password))
                {
                    return Encoding.ASCII.GetBytes("login failed");
                }

                var dirName = (Path.GetDirectoryName(filePath) ?? string.Empty).Replace('\\', '/');
                if (!dirName.StartsWith(Constants.MediaLibraryPath))
                {
                    dirName = Constants.MediaLibraryPath + (dirName.StartsWith("/") ? dirName : "/" + dirName);
                }
                var itemname = dirName + "/" + Path.GetFileNameWithoutExtension(filePath);
                var db = Factory.GetDatabase(database);
                var item = (MediaItem)db.GetItem(itemname);
                using (var stream = item.GetMediaStream())
                {
                    var result = new byte[stream.Length];
                    stream.Read(result, 0, (int)stream.Length);
                    return result;
                }
            }
            catch (Exception ex)
            {
                PowerShellLog.Error("Error during uploading file using PowerShell web service", ex);
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