using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.UI;
using Cognifide.PowerShell.PowerShellIntegrations;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Form.UI.Controls;
using Sitecore.Install.Utils;
using Sitecore.Shell.Applications.Layouts.IDE.Editors.Xslt;
using Sitecore.Shell.Applications.WebEdit;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace Cognifide.PowerShell.SitecoreIntegrations.Commands
{
    [Serializable]
    public class RebuildAllIntegrations : Command
    {

        protected const string integrationsPath = "/sitecore/content/Applications/PowerShell/PowerShellIse/Menus/Integrations";

        public override CommandState QueryState(CommandContext context)
        {
            return CommandState.Enabled;
        }


        /// <summary>
        ///     Executes the command in the specified context.
        /// </summary>
        /// <param name="context">
        ///     The context.
        /// </param>
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Database coreDb = Factory.GetDatabase("core");
            Database masterDb = Factory.GetDatabase("master");

            // create or update script references for existing scripts
            var integrationsRoot = masterDb.GetItem(integrationsPath);
            if (integrationsRoot != null)
            {
                foreach (Item integrationItem in integrationsRoot.Children)
                {
                    var sectionName = integrationItem.Name;
                }
            }
        }
    }
}