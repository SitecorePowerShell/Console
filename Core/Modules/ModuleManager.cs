using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Modules
{
    public static class ModuleManager
    {
        public delegate void InvalidateEventHandler(object sender, EventArgs e);

        public static event InvalidateEventHandler OnInvalidate;

        private static List<Module> modules;

        public static List<Module> Modules
        {
            get
            {
                if (modules == null)
                {
                    modules = new List<Module>();
                    var dbModules = GetDbModules(ApplicationSettings.ScriptLibraryDb);
                    modules.AddRange(dbModules);

                    dbModules = GetDbModules("core");
                    modules.AddRange(dbModules);
                }
                return modules;
            }
        }

        public static List<Module> GetDbModules(string database)
        {
            var dbModules = new List<Module>();

            var db = Factory.GetDatabase(database);
            if (db != null)
            {
                Item library = db.GetItem(ApplicationSettings.ScriptLibraryPath);
                if (library != null)
                {
                    dbModules.Add(new Module(library, true));

                    foreach (Item item in library.GetChildren())
                    {
                        if (item.TemplateName.Equals("PowerShell Script Module", StringComparison.InvariantCulture))
                        {
                            dbModules.Add(new Module(item, false));
                        }
                    }
                }
            }
            return dbModules;
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

        public static List<Item> GetFeatureRoots(string featureName, string dbName)
        {
            List<Item> list = new List<Item>();
            var modules = GetDbModules(dbName);
            foreach (Module module in modules)
            {
                Item featureRoot = module.GetFeatureRoot(featureName);
                if (featureRoot != null) list.Add(featureRoot);
            }
            return list;
        }


        public static void Invalidate(Item item)
        {
            modules = null;
            if (OnInvalidate != null)
            {
                OnInvalidate(null, EventArgs.Empty);
            }
        }

        public static Module GetItemModule(Item item)
        {
            if (item.TemplateName.Equals(TemplateNames.ScriptModuleTemplateName, StringComparison.InvariantCulture))
            {
                return GetModule(item);
            }

            if (item.TemplateName.Equals(TemplateNames.ScriptLibraryTemplateName, StringComparison.InvariantCulture))
            {
                if (string.Equals(item.Name, "Script Library"))
                {
                    return GetModule(item);
                }
                return GetItemModule(item.Parent);
            }
            if (item.TemplateName.Equals(TemplateNames.ScriptTemplateName, StringComparison.InvariantCulture))
            {
                return GetItemModule(item.Parent);
            }
            return null;
        }

        public static Module GetModule(string moduleName)
        {
            return
                Modules.FirstOrDefault(
                    module => string.Equals(module.Name, moduleName, StringComparison.InvariantCultureIgnoreCase));
        }

        private static Module GetModule(Item item)
        {
            return
                Modules.FirstOrDefault(
                    module =>
                        module.ID == item.ID &&
                        string.Equals(module.Database, item.Database.Name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}