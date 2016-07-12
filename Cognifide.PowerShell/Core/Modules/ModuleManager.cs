using System;
using System.Collections.Generic;
using System.Linq;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.SecurityModel;

namespace Cognifide.PowerShell.Core.Modules
{
    public static class ModuleManager
    {
        public delegate void InvalidateEventHandler(object sender, EventArgs e);

        private static List<Module> modules;
        private static volatile bool modulesListDirty = true;
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
            using (new SecurityDisabler())
            {
                var db = Factory.GetDatabase(database);
                var library = db?.GetItem(ApplicationSettings.ScriptLibraryPath);
                if (library != null)
                {
                    dbModules.Add(new Module(library, true));

                    EnumerateLibraries(library, dbModules);
                }
            }
            return dbModules;
        }

        private static void EnumerateLibraries(Item library, List<Module> dbModules)
        {
            foreach (Item item in library.GetChildren())
            {
                if (item.IsPowerShellModule())
                {
                    dbModules.Add(new Module(item, false));
                }
                if (item.IsPowerShellModuleFolder())
                {
                    EnumerateLibraries(item, dbModules);
                }
            }
        }

        public static List<Item> GetFeatureRoots(string featureName)
        {
            return
                Modules
                    .Select(module => module.GetFeatureRoot(featureName))
                    .Where(featureRoot => featureRoot != null)
                    .ToList();
        }

        public static List<Item> GetFeatureRoots(string featureName, string dbName)
        {
            var modules = GetDbModules(dbName);
            return
                modules
                    .Select(module => module.GetFeatureRoot(featureName))
                    .Where(featureRoot => featureRoot != null)
                    .ToList();
        }

        public static void Invalidate(Item item)
        {
            modulesListDirty = true;
            OnInvalidate?.Invoke(null, EventArgs.Empty);
        }

        public static Module GetItemModule(Item item)
        {
            if (item.IsPowerShellModule())
            {
                return GetModule(item);
            }

            if (item.IsPowerShellLibrary())
            {
                return string.Equals(item.Name, "Script Library") ? GetModule(item) : GetItemModule(item.Parent);
            }
            return item.IsPowerShellScript()
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