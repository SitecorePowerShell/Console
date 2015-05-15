using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Extensions;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider
    {
        public void GetProperty(string path, Collection<string> providerSpecificPickList)
        {
            try
            {
                LogInfo("Executing GetProperty(string path='{0}', Collection<string> providerSpecificPickList)", path);

                var item = GetItemForPath(path);
                if (item != null)
                {
                    // create PSObject from the FileSystemInfo instance
                    var psobj = ItemShellExtensions.GetPsObject(SessionState, item);

                    // create the PSObject to copy properties into and that we will return
                    var result = new PSObject();

                    foreach (var name in providerSpecificPickList)
                    {
                        // Copy all the properties from the original object into ’result’
                        var prop = psobj.Properties[name];
                        object value = null;
                        if (prop != null)
                        {
                            value = prop.Value;
                        }
                        else
                        {
                            WriteWarning(String.Format("Property name ’{0}’ doesn’t exist for item at path ’{1}’", name,
                                path));
                        }
                        result.Properties.Add(new PSNoteProperty(name, value));
                    }

                    foreach (var name in providerSpecificPickList)
                    {
                        var field = item.Fields[name];
                        if (field != null)
                        {
                            result.Properties[name].Value = field.Value;
                        }
                    }

                    WritePropertyObject(result, path);
                }
            }
            catch (Exception ex)
            {
                LogError(ex,
                    "Error while executing GetProperty(string path='{0}', Collection<string> providerSpecificPickList)",
                    path);
                throw;
            }
        }

        public void SetProperty(string path, PSObject propertyToSet)
        {
            try
            {
                if (propertyToSet == null)
                {
                    throw new ArgumentNullException("Property not defined");
                }

                LogInfo("Executing SetProperty(string path='{0}', PSObject propertyToSet='{1}')", path, propertyToSet);
                var item = GetItemForPath(path);
                if (item != null)
                {
                    foreach (PSPropertyInfo property in propertyToSet.Properties)
                    {
                        if (ShouldProcess(path,
                            "Setting property '" + property.Name + "' to '" + property.Value + "'"))
                        {
                            item.Fields.ReadAll();
                            if (item.Fields != null && item.Fields[property.Name] != null)
                            {
                                ItemShellExtensions.ModifyProperty(item, property.Name, property.Value);
                            }
                            else
                            {
                                WriteWarning(String.Format("Property name ’{0}’ doesn’t exist for item at path ’{1}’",
                                    property.Name,
                                    path));
                            }
                            WriteItem(item);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(
                    string.Format(
                        "Error while executing GetProperty(string path='{0}', PSObject propertyValue)",
                        path), ex);
                throw;
            }
        }

        public void ClearProperty(string path, Collection<string> propertyToClear)
        {
            try
            {
                LogInfo("Executing ClearProperty(string path='{0}', string propertyToClear='{1}')",
                    path,
                    propertyToClear.Aggregate((seed, curr) => seed + ',' + curr));
                var item = GetItemForPath(path);
                item.Edit(args =>
                {
                    foreach (var property in propertyToClear)
                    {
                        if (ShouldProcess(path,
                            "Restoring property '" + property + "' to default set on Standard Values"))
                        {
                            item.Fields[property].Reset();
                        }
                    }
                });
            }

            catch (Exception ex)
            {
                LogError(ex, "Error while executing ClearProperty(string path='{0}', string propertyToClear='{1}')",
                    path,
                    propertyToClear.Aggregate((seed, curr) => seed + ',' + curr));
                throw;
            }
        }
    }
}