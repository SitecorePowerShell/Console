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

        private static List<Module> modules;
        private volatile static bool modulesListDirty = true;
        private static readonly object modulesLock = new object();

        public static List<Module> Modules
        {
            get
            {
                if (modulesListDirty || modules == null)
                {
                    lock (modulesLock)
                    {
                        if (modulesListDirty || modules == null)
                        {
                            modulesListDirty = false;
                            var newModulesList = GetDbModules(ApplicationSettings.ScriptLibraryDb);
                            newModulesList.AddRange(GetDbModules("core"));
                            modules = newModulesList;
                        }
                    }
                }
                return modules;
            }
        }

        public static event InvalidateEventHandler OnInvalidate;

        public static List<Module> GetDbModules(string database)
        {
            var dbModules = new List<Module>();

            var db = Factory.GetDatabase(database);
            if (db != null)
            {
                var library = db.GetItem(ApplicationSettings.ScriptLibraryPath);
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
            var list = new List<Item>();
            foreach (var module in Modules)
            {
                var featureRoot = module.GetFeatureRoot(featureName);
                if (featureRoot != null) list.Add(featureRoot);
            }
            return list;
        }

        public static List<Item> GetFeatureRoots(string featureName, string dbName)
        {
            var list = new List<Item>();
            var modules = GetDbModules(dbName);
            foreach (var module in modules)
            {
                var featureRoot = module.GetFeatureRoot(featureName);
                if (featureRoot != null) list.Add(featureRoot);
            }
            return list;
        }

        public static void Invalidate(Item item)
        {
            modulesListDirty = true;
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
                return string.Equals(item.Name, "Script Library") ? GetModule(item) : GetItemModule(item.Parent);
            }
            return item.TemplateName.Equals(TemplateNames.ScriptTemplateName, StringComparison.InvariantCulture)
                ? GetItemModule(item.Parent)
                : null;
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