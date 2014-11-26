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
            List<Item> list = new List<Item>();
            foreach (Module module in Modules)
            {
                Item featureRoot = module.GetFeatureRoot(featureName);
                if (featureRoot != null) list.Add(featureRoot);
            }
            return list;
        }

        public static void Invalidate(Item item)
        {
            modules = null;
/*
            foreach (var module in Modules)
            {
                if (module.Database != item.Database.Name &&
                    (item.Paths.Path.IndexOf(module.Path, StringComparison.InvariantCultureIgnoreCase) == 0) ||
                    (item.ID == module.ID))
                {
                    module.Invalidate();
                }
            }
*/
        }
    }
}