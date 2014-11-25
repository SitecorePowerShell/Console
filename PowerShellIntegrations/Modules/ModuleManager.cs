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
        private static SortedList<string, Module> modules = null;

        public static SortedList<string, Module> Modules
        {
            get
            {
                if (modules == null)
                {
                    Item masterLibrary = Factory.GetDatabase(ApplicationSettings.ScriptLibraryDb).GetItem(ScriptLibrary.Path);
                    Item coreLibrary = Factory.GetDatabase("core").GetItem(ScriptLibrary.Path);
                    modules = new SortedList<string, Module>
                    {
                        {ApplicationSettings.ScriptLibraryDb+":root", new Module(masterLibrary,true)},
                        {"core:root", new Module(coreLibrary,true)}
                    };
                    foreach (Item item in masterLibrary.GetChildren())
                    {
                        if (item.TemplateName.Equals("PowerShell Script Module", StringComparison.InvariantCulture))
                        {
                            modules.Add(item.Database.Name+":"+item.Name, new Module(item,false));
                        }
                    }
                }
                return modules;
            }
        }
    }
}