using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Authentication;
using System.Web.Services;
using Cognifide.PowerShell.PowerShellIntegrations.Host;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Sitecore.Security.Authentication;

namespace Cognifide.PowerShell.Console.Services
{
    /// <summary>
    ///     Summary description for RemoteAutomation
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class RemoteAutomation : WebService
    {
        [WebMethod]
        public NameValue[] ExecuteScript(string userName, string password, string script, string returnVariables)
        {
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }
                bool loggedIn = AuthenticationManager.Login(userName, password, false);
                if (!loggedIn)
                {
                    throw new AuthenticationException("Unrecognized user or password mismatch.");
                }
            }

            using (var scriptSession = new ScriptSession(ApplicationNames.RemoteAutomation, false))
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
            if (!string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(password))
            {
                if (!userName.Contains("\\"))
                {
                    userName = "sitecore\\" + userName;
                }
                bool loggedIn = AuthenticationManager.Login(userName, password, false);
                if (!loggedIn)
                {
                    throw new AuthenticationException("Unrecognized user or password mismatch.");
                }
            }

            using (var scriptSession = new ScriptSession(ApplicationNames.RemoteAutomation, false))
            {
                scriptSession.SetVariable("cliXmlArgs", cliXmlArgs);
                scriptSession.ExecuteScriptPart(scriptSession.Settings.Prescript);
                scriptSession.ExecuteScriptPart("$params = ConvertFrom-CliXml -InputObject $cliXmlArgs");
                scriptSession.ExecuteScriptPart(script + "| ConvertTo-CliXml");

                return scriptSession.Output.Select(p => p.Terminated ? p.Text + "\n" : p.Text).Aggregate(
                                (current, next) => current + next);
            }
        }

        public class NameValue
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }
}