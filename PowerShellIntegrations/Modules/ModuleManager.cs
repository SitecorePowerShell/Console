using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Cognifide.PowerShell.PowerShellIntegrations.Settings;
using Microsoft.SqlServer.Server;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Install.Items;

namespace Cognifide.PowerShell.PowerShellIntegrations.Modules
{
    public static class ModuleManager
    {
        private static List<Module> modules;

        public static List<Module> Modules
        {
            get
            {
                if (modules == null)
                {
                    Item masterLibrary = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb).GetItem(ScriptLibrary.Path);
                    Item coreLibrary = Factory.GetDatabase("core").GetItem(ScriptLibrary.Path);
                    modules = new List<Module>
                    {
                        new Module(masterLibrary,true),
                        new Module(coreLibrary,true)
                    };
                    foreach (Item item in masterLibrary.GetChildren())
                    {
                        if (item.TemplateName.Equals("PowerShell Script Module", StringComparison.InvariantCulture))
                        {
                            modules.Add(new Module(item,false));
                        }
                    }
                }
                return modules;
            }
        }

        public static List<Item> GetFeatureRoots(string featureName)
        {
            return
                Modules.Select(module => module.GetFeatureRoot(featureName))
                    .Where(featureRoot => featureRoot != null)
                    .ToList();
        }
    }
}