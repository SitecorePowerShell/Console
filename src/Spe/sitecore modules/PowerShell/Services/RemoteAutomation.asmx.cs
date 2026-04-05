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

        private static RestrictionProfile ValidateAndResolveProfile(string script, string userName)
        {
            // Service-level command restrictions
            if (!ScriptValidator.ValidateScript(WebServiceSettings.ServiceRemoting, script, null, out var blockedCommand))
            {
                PowerShellLog.Audit("[Remoting(SOAP)] action=scriptRejected user={0} ip={1} blockedCommand={2}",
                    userName, GetIp(), blockedCommand);
                throw new InvalidOperationException($"Script contains blocked command: {blockedCommand}");
            }

            // Resolve restriction profile for the remoting service (SOAP has no JWT scope or API key)
            var profile = RestrictionProfileManager.ResolveProfile(WebServiceSettings.ServiceRemoting, null, null);
            if (profile != null && profile != RestrictionProfile.Unrestricted)
            {
                if (!ScriptValidator.ValidateScriptAgainstProfile(profile, script, userName, WebServiceSettings.ServiceRemoting, out var profileBlockedCommand))
                {
                    PowerShellLog.Audit("[Remoting(SOAP)] action=scriptRejectedByProfile user={0} ip={1} profile={2} blockedCommand={3}",
                        userName, GetIp(), profile.Name, profileBlockedCommand);
                    throw new InvalidOperationException($"Script blocked by restriction profile '{profile.Name}': {profileBlockedCommand}");
                }
            }

            return profile;
        }

        private static PSLanguageMode ApplyRestrictions(ScriptSession session, RestrictionProfile profile)
        {
            session.ActiveRestrictionProfile = profile;
            var languageMode = WebServiceSettings.GetLanguageMode(WebServiceSettings.ServiceRemoting);

            if (profile != null && profile != RestrictionProfile.Unrestricted)
            {
                // Profile's language mode overrides when more restrictive
                if (profile.LanguageMode > languageMode)
                {
                    languageMode = profile.LanguageMode;
                }

                // Module restrictions
                if (profile.Modules != null && profile.Modules.RestrictModules)
                {
                    session.SetVariable("PSModuleAutoloadingPreference", profile.Modules.AutoloadPreference);
                }
            }

            if (languageMode != PSLanguageMode.FullLanguage)
            {
                session.SetLanguageMode(languageMode);
            }

            return languageMode;
        }

        private static void RestoreRestrictions(ScriptSession session, PSLanguageMode appliedMode, RestrictionProfile profile)
        {
            if (appliedMode != PSLanguageMode.FullLanguage)
            {
                session.SetLanguageMode(PSLanguageMode.FullLanguage);
            }

            if (profile?.Modules != null && profile.Modules.RestrictModules)
            {
                session.RemoveVariable("PSModuleAutoloadingPreference");
            }
        }

        private bool Login(string userName, string password)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                PowerShellLog.Audit($"[Remoting(SOAP)] action=loginAttempt user={userName}");

                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }

                if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceRemoting,userName))
                {
                    PowerShellLog.Audit($"[Remoting(SOAP)] action=userUnauthorized user={userName}");
                    return false;
                }

                var loggedIn = AuthenticationManager.Login(userName, password, false);
                if (!loggedIn)
                {
                    PowerShellLog.Audit($"[Remoting(SOAP)] action=authFailed user={userName}");
                }
                else
                {
                    PowerShellLog.Audit($"[Remoting(SOAP)] action=loginSuccess user={userName}");
                }
                return loggedIn;
            }
            PowerShellLog.Audit($"[Remoting(SOAP)] action=loginFailed user={userName} reason=emptyCredentials");
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

            PowerShellLog.Audit($"[Remoting(SOAP)] action=scriptExecuting user={userName} sessionType=disposable");

            using (var scriptSession = ScriptSessionManager.NewSession(ApplicationNames.RemoteAutomation, false))
            {
                var profile = ValidateAndResolveProfile(script, userName);

                var scriptHash = ComputeScriptHash(script);
                PowerShellLog.Audit("[Remoting(SOAP)] action=scriptStarting user={0} ip={1} session={2} scriptHash={3} profile={4}",
                    userName, GetIp(), scriptSession.ID, scriptHash, profile?.Name ?? "unrestricted");

                var appliedMode = ApplyRestrictions(scriptSession, profile);
                try
                {
                    scriptSession.ExecuteScriptPart(script);
                }
                finally
                {
                    RestoreRestrictions(scriptSession, appliedMode, profile);
                }

                PowerShellLog.Audit("[Remoting(SOAP)] action=scriptCompleted user={0} ip={1} session={2} scriptHash={3}",
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

            PowerShellLog.Audit($"[Remoting(SOAP)] action=sessionDisposed session={sessionId}");

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

            PowerShellLog.Audit($"[Remoting(SOAP)] action=scriptExecuting user={userName} session={sessionId} sessionType=persistent");

            var scriptSession = ScriptSessionManager.GetSession(sessionId, ApplicationNames.RemoteAutomation, false);

            var profile = ValidateAndResolveProfile(script, userName);

            var scriptHash = ComputeScriptHash(script);
            PowerShellLog.Audit("[Remoting(SOAP)] action=scriptStarting user={0} ip={1} session={2} scriptHash={3} profile={4}",
                userName, GetIp(), scriptSession.ID, scriptHash, profile?.Name ?? "unrestricted");

            Sitecore.Context.SetActiveSite(siteName);

            if (!string.IsNullOrEmpty(cliXmlArgs))
            {
                scriptSession.SetVariable("cliXmlArgs", cliXmlArgs);
                scriptSession.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs", false, true);
                script = script.TrimEnd(' ', '\t', '\n');
            }

            var appliedMode = ApplyRestrictions(scriptSession, profile);
            List<object> outObjects;
            try
            {
                outObjects = scriptSession.ExecuteScriptPart(script, false, false, false);
            }
            finally
            {
                RestoreRestrictions(scriptSession, appliedMode, profile);
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
                    PowerShellLog.Audit($"[Remoting(SOAP)] action=pathTraversalBlocked user={userName} path=\"{filePath}\"");
                    return false;
                }

                PowerShellLog.Audit($"[Remoting(SOAP)] action=mediaUploaded user={userName} path=\"{filePath}\"");

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
                PowerShellLog.Error("[Remoting(SOAP)] action=mediaUploadFailed", ex);
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
                    PowerShellLog.Audit($"[Remoting(SOAP)] action=pathTraversalBlocked user={userName} path=\"{filePath}\"");
                    return new byte[0];
                }

                PowerShellLog.Audit($"[Remoting(SOAP)] action=mediaDownloaded user={userName} path=\"{filePath}\"");

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
                PowerShellLog.Error("[Remoting(SOAP)] action=mediaDownloadFailed", ex);
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