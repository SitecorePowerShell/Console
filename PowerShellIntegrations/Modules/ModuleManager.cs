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
                    Item masterLibrary = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb).GetItem(ScriptLibrary.Path);
                    if (masterLibrary != null)
                    {
                        modules.Add(new Module(masterLibrary,true));
                    }

                    Item coreLibrary = Factory.GetDatabase("core").GetItem(ScriptLibrary.Path);
                    if (coreLibrary  != null)
                    {
                        modules.Add(new Module(coreLibrary,true));
                    }
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