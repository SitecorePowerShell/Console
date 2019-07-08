using System;
using System.Linq;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.GetMasters;
using Spe.Core.Diagnostics;
using Spe.Core.Extensions;
using Spe.Core.Modules;
using Spe.Core.Settings;
using Spe.Core.Settings.Authorization;
using Spe.Core.Utility;

namespace Spe.Integrations.Pipelines
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

        private static void GetMasters(Item parent, GetMastersArgs args)
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