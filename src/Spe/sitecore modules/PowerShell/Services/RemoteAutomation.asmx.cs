using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Services;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.Resources.Media;
using Sitecore.Security.Authentication;
using Sitecore.Sites;
using Sitecore.Web;
using Spe.Core.Diagnostics;
using Spe.Core.Host;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;

namespace Spe.sitecore_modules.PowerShell.Services
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
        private static string ComputeScriptHash(string script)
        {
            if (string.IsNullOrEmpty(script)) return "empty";
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(script));
                return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant().Substring(0, 16);
            }
        }

        private static string GetIp()
        {
            var request = HttpContext.Current?.Request;
            if (request == null) return "unknown";
            var ip = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            return string.IsNullOrEmpty(ip) ? request.ServerVariables["REMOTE_ADDR"] : ip;
        }

        // SOAP uses basic auth (username/password), not Bearer tokens, so there is no JWT
        // to extract scope from. Scope-based restrictions only apply to REST (Bearer) endpoints.
        // Pass null for scope -- only service-level command restrictions are enforced here.
        private static void ValidateScript(string script, string userName)
        {
            if (!ScriptValidator.ValidateScript(WebServiceSettings.ServiceRemoting, script, null, out var blockedCommand))
            {
                PowerShellLog.Audit("Remoting(SOAP): script rejected, user={0}, ip={1}, blockedCommand={2}, clientSession=none",
                    userName, GetIp(), blockedCommand);
                throw new InvalidOperationException($"Script contains blocked command: {blockedCommand}");
            }
        }

        private static PSLanguageMode ApplyLanguageMode(ScriptSession session)
        {
            var languageMode = WebServiceSettings.GetLanguageMode(WebServiceSettings.ServiceRemoting);
            if (languageMode != PSLanguageMode.FullLanguage)
            {
                session.SetLanguageMode(languageMode);
            }
            return languageMode;
        }

        private static void RestoreLanguageMode(ScriptSession session, PSLanguageMode appliedMode)
        {
            if (appliedMode != PSLanguageMode.FullLanguage)
            {
                session.SetLanguageMode(PSLanguageMode.FullLanguage);
            }
        }

        private bool Login(string userName, string password)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                PowerShellLog.Info($"User '{userName}' calling the Remoting Automation service.");

                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }

                if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceRemoting,userName))
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
            if (!WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting))
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

            PowerShellLog.Info($"Script executed through remoting by user: '{userName}' in disposable session.");

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                ValidateScript(script, userName);

                var scriptHash = ComputeScriptHash(script);
                PowerShellLog.Audit("Remoting(SOAP): script starting, user={0}, ip={1}, session={2}, scriptHash={3}",
                    userName, GetIp(), scriptSession.ID, scriptHash);

                var appliedMode = ApplyLanguageMode(scriptSession);
                try
                {
                    scriptSession.ExecuteScriptPart(script);
                }
                finally
                {
                    RestoreLanguageMode(scriptSession, appliedMode);
                }

                PowerShellLog.Audit("Remoting(SOAP): script completed, user={0}, ip={1}, session={2}, scriptHash={3}",
                    userName, GetIp(), scriptSession.ID, scriptHash);

                var result = new List<NameValue>();

                if (scriptSession.Output.Count > 0)
                {
                    result.Add(new NameValue
                    {
                        Name = "output",
                        Value = string.Join("", scriptSession.Output.Select(p => p.Terminated ? p.Text + "\n" : p.Text))
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
            if (!WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting))
            {
                return string.Empty;
            }

            if (!Login(userName, password))
            {
                return "login failed";
            }

            PowerShellLog.Info($"Session '{sessionId}' disposed by user: '{Sitecore.Context.User?.Name}'");

            if (ScriptSessionManager.GetSessionIfExists(sessionId) is ScriptSession session)
            {
                ScriptSessionManager.RemoveSession(session);
                return "removed";
            }

            return "not found";
        }

        [WebMethod]
        public string ExecuteScriptBlockinSite2(string userName, string password, string script, string cliXmlArgs,
            string siteName, string sessionId)
        {
            if (!WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting))
            {
                return string.Empty;
            }
            if (!Login(userName, password))
            {
                return "<Objs xmlns=\"http://schemas.microsoft.com/powershell/2004/04\"><Obj RefId=\"0\"><S>login failed</S></Obj></Objs>";
            }

            PowerShellLog.Info($"Script executed in session {sessionId} through remoting by user: '{userName}'");

            var scriptSession = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            ValidateScript(script, userName);

            var scriptHash = ComputeScriptHash(script);
            PowerShellLog.Audit("Remoting(SOAP): script starting, user={0}, ip={1}, session={2}, scriptHash={3}",
                userName, GetIp(), scriptSession.ID, scriptHash);

            Sitecore.Context.SetActiveSite(siteName);

            if (!string.IsNullOrEmpty(cliXmlArgs))
            {
                scriptSession.SetVariable("cliXmlArgs", cliXmlArgs);
                scriptSession.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs", false, true);
                script = script.TrimEnd(' ', '\t', '\n');
            }

            var appliedMode = ApplyLanguageMode(scriptSession);
            List<object> outObjects;
            try
            {
                outObjects = scriptSession.ExecuteScriptPart(script, false, false, false);
            }
            finally
            {
                RestoreLanguageMode(scriptSession, appliedMode);
            }
            if (scriptSession.LastErrors != null && scriptSession.LastErrors.Any())
            {
                outObjects.AddRange(scriptSession.LastErrors);
            }
            scriptSession.SetVariable("results", outObjects);
            scriptSession.Output.Clear();
            scriptSession.ExecuteScriptPart("ConvertTo-CliXml -InputObject $results");
            var result = string.Join("", scriptSession.Output.Select(p => p.Text));

            if (string.IsNullOrEmpty(sessionId))
            {
                ScriptSessionManager.RemoveSession(scriptSession);
            }
            return result;
        }

        [WebMethod]
        public bool UploadFile(string userName, string password, string filePath, byte[] fileContent, string database,
            string language)
        {
            if (!WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting))
            {
                return false;
            }

            try
            {
                if (!Login(userName, password))
                {
                    return false;
                }

                if (filePath.Contains(".."))
                {
                    PowerShellLog.Error($"Rejected file path with traversal attempt: '{filePath}'");
                    return false;
                }

                PowerShellLog.Info($"File '{filePath}' uploaded through remoting by user: '{userName}'");

                var dirName = (Path.GetDirectoryName(filePath) ?? string.Empty).Replace('\\', '/');
                if (!dirName.StartsWith(Constants.MediaLibraryPath))
                {
                    dirName = Constants.MediaLibraryPath + (dirName.StartsWith("/") ? dirName : "/" + dirName);
                }

                var mco = new MediaCreatorOptions
                {
                    Database = Factory.GetDatabase(database),
                    Language = Language.Parse(language),
                    Versioned = Settings.Media.UploadAsVersionableByDefault,
                    Destination = $"{dirName}/{Path.GetFileNameWithoutExtension(filePath)}"
                };

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
            if (!WebServiceSettings.IsEnabled(WebServiceSettings.ServiceRemoting))
            {
                return new byte[0];
            }

            try
            {
                if (!Login(userName, password))
                {
                    return Encoding.ASCII.GetBytes("login failed");
                }

                if (filePath.Contains(".."))
                {
                    PowerShellLog.Error($"Rejected file path with traversal attempt: '{filePath}'");
                    return new byte[0];
                }

                PowerShellLog.Info($"File '{filePath}' downloaded through remoting by user: '{userName}'");

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