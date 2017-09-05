using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.Client.Commands.MenuItems;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Modules;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Settings.Authorization;
using Cognifide.PowerShell.Core.Utility;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetMasters;
using Sitecore.Web.UI.HtmlControls;

namespace Cognifide.PowerShell.Integrations.Pipelines
{
    public class GetItemMasters
    {
        public void Process(GetMastersArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            try
            {
                if (!ServiceAuthorizationManager.IsUserAuthorized(WebServiceSettings.ServiceExecution, Context.User.Name))
                {
                    return;
                }


                var roots = ModuleManager.GetFeatureRoots(IntegrationPoints.ContentEditorInsertItemFeature);

                foreach (var root in ModuleManager.GetFeatureRoots(IntegrationPoints.ContentEditorInsertItemFeature))
                {
                    GetMasters(root, args);
                }

            }
            catch (Exception e)
            {
                PowerShellLog.Error("Exception while getting item Masters", e);
            }
        }

        private void GetMasters(Item parent, GetMastersArgs args)
        {
            foreach (var scriptItem in parent.Children.Where(p => p.IsPowerShellScript() || p.IsPowerShellLibrary()))
            {
                if (!RulesUtils.EvaluateRules(scriptItem[FieldNames.ShowRule], args.Item))
                {
                    continue;
                }

                if (scriptItem.IsPowerShellScript())
                {
                    args.Masters.Add(scriptItem);
                }
                else
                {
                    GetMasters(scriptItem,args);
                }
            }
        }
    }
}