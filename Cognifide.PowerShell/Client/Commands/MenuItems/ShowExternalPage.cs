using System;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Core.Host;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Shell.Framework;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.Client.Commands.MenuItems
{
    [Serializable]
    public class ShowExternalPage : Command
    {
        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }

        public override void Execute(CommandContext context)
        {
            var urlString = new UrlString(UIUtil.GetUri("control:PowerShellExternalView"));
            var keys = context.Parameters.AllKeys;

            using (var session = ScriptSessionManager.GetSession(string.Empty, ApplicationNames.Default, false))
            {
                session.ExecuteScriptPart("");
                foreach (var key in keys)
                {
                    var param =
                        context.Parameters[key]
                            .Replace("{spe}", CurrentVersion.SpeVersion.ToString())
                            .Replace("{ps}", ScriptSession.PsVersion.Major + "." + ScriptSession.PsVersion.Minor)
                            .Replace("{sc}",
                                SitecoreVersion.Current.Major + "." +
                                SitecoreVersion.Current.Minor);
                    urlString.Add(key, param);
                }

            }

            var width = keys.Contains("spe_w", StringComparer.OrdinalIgnoreCase)
                ? context.Parameters["spe_w"]
                : keys.Contains("width", StringComparer.OrdinalIgnoreCase) ? context.Parameters["width"] : "800";
            
            var height = keys.Contains("spe_h", StringComparer.OrdinalIgnoreCase)
                ? context.Parameters["spe_h"]
                : keys.Contains("height", StringComparer.OrdinalIgnoreCase) ? context.Parameters["height"] : "800";

            SheerResponse.ShowModalDialog(urlString.ToString(), width, height);
        }
    }
}