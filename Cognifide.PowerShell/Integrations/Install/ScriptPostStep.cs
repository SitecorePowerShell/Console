using System.Collections.Specialized;
using Cognifide.PowerShell.Core.VersionDecoupling;
using Cognifide.PowerShell.Core.VersionDecoupling.Interfaces;
using Sitecore;
using Sitecore.Install.Framework;
using Sitecore.Jobs.AsyncUI;
using Sitecore.Text;

namespace Cognifide.PowerShell.Integrations.Install
{
    public class ScriptPostStep : IPostStep
    {
        public void Run(ITaskOutput output, NameValueCollection metaData)
        {
            var attributes = new AttributesContainer(metaData["Attributes"]);
            var scriptId = attributes.Get("scriptId")?.ToString();
            var scriptDb = attributes.Get("scriptDb")?.ToString() ?? "master";

            var width = attributes.Get("width")?.ToString() ?? "400";
            var height = attributes.Get("height")?.ToString() ?? "260";

            var str = new UrlString(UIUtil.GetUri("control:PowerShellRunner"));
            str.Append("scriptId", scriptId);
            str.Append("scriptDb", scriptDb);
            var jobUiManager = TypeResolver.Resolve<IJobUiManager>();
            jobUiManager.ShowModalDialog(str.ToString(), width, height);
        }
    }
}