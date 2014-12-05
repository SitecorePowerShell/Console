using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.sitecore.shell.ClientBin.Dashboard;

namespace Cognifide.PowerShell.PowerShellIntegrations.Modules
{
    public class Module
    {
        public bool AlwaysEnabled { get; private set; }
        public bool Enabled { get; private set; }
        public string Database { get; private set; }
        public string Path { get; private set; }
        public ID ID { get; private set; }
        public SortedList<string, ID> Features { get; private set; }
        public string Name { get; private set; }
        public string Description { get; private set; }
        public string WhatsNew { get; private set; }
        public string History { get; private set; }
        public string Version { get; private set; }
        public string Author { get; private set; }
        public string Url { get; private set; }
        public string Email { get; private set; }
        public string Support { get; private set; }
        public string License { get; private set; }
        public string PublishedBy { get; private set; }
        public string Category { get; private set; }

        public Module(Item moduleItem, bool alwaysEnabled)
        {
            AlwaysEnabled = alwaysEnabled;
            ID = moduleItem.ID;
            Database = moduleItem.Database.Name;
            Update(moduleItem);
        }


        private void Update(Item moduleItem)
        {
            Path = moduleItem.Paths.Path;
            Enabled = AlwaysEnabled || moduleItem["Enabled"] == "1";
            Features = new SortedList<string, ID>(StringComparer.OrdinalIgnoreCase);
            Name = moduleItem.Name;
            if (Enabled)
            {
                Description = moduleItem["Description"];
                WhatsNew = moduleItem["WhatsNew"];
                History = moduleItem["History"];
                Version = moduleItem["Version"];
                Author = moduleItem["Author"];
                Url = moduleItem["Url"];
                Email = moduleItem["Email"];
                Support = moduleItem["Support"];
                License = moduleItem["License"];
                PublishedBy = moduleItem["PublishedBy"];
                Category = moduleItem["Category"];
                foreach (var integrationPoint in IntegrationPoints.Libraries)
                {
                    Item featureItem = moduleItem.Database.GetItem(moduleItem.Paths.Path + "/" + integrationPoint.Value.Path);
                    if (featureItem != null)
                    {
                        Features.Add(integrationPoint.Key, featureItem.ID);
                    }
                }
            }
        }

        public Item GetFeatureRoot(string integrationPoint)
        {
            if (!string.IsNullOrEmpty(integrationPoint))
            {
                if (Features.Keys.Contains(integrationPoint))
                {
                    return Factory.GetDatabase(Database).GetItem(Features[integrationPoint]);
                }
            }
            return null;
        }

        public string GetFeaturePath(string integrationPoint)
        {
            if (!string.IsNullOrEmpty(integrationPoint))
            {
                if (IntegrationPoints.Libraries.Keys.ToList()
                    .Contains(integrationPoint, StringComparer.OrdinalIgnoreCase))
                {
                    return Path + "/" + IntegrationPoints.Libraries[integrationPoint].Path;
                }
            }
            return string.Empty;
        }

        public string GetProviderFeaturePath(string integrationPoint)
        {
            if (!string.IsNullOrEmpty(integrationPoint))
            {
                if (IntegrationPoints.Libraries.Keys.ToList()
                    .Contains(integrationPoint, StringComparer.OrdinalIgnoreCase))
                {
                    return string.Format("{0}:{1}/{2}", Database, Path, IntegrationPoints.Libraries[integrationPoint].Path);
                }
            }
            return string.Empty;
        }
        public string GetCreateScript(string integrationPoint)
        {
            if (IntegrationPoints.Libraries.Keys.ToList().Contains(integrationPoint, StringComparer.OrdinalIgnoreCase))
            {
                return Path + "/" + IntegrationPoints.Libraries[integrationPoint].CreationScript;
            }
            return string.Empty;
        }

        public void Invalidate()
        {
            Update(Factory.GetDatabase(Database).GetItem(ID));
        }
    }
}