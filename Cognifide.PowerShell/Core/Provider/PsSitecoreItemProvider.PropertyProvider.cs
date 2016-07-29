using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Cognifide.PowerShell.Core.Diagnostics;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Utility;
using Sitecore.Data.Items;
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

                var item = GetDynamicItem(path);
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
                var item = GetDynamicItem(path);

                if (propertyToSet == null)
                {
                    throw new ArgumentNullException("Property not defined");
                }

                LogInfo("Executing SetProperty(string path='{0}', PSObject propertyToSet='{1}')", path, propertyToSet);
                if (item != null)
                {
                    item.Edit(args =>
                    {
                        item.Fields.ReadAll();

                        var asDict = propertyToSet.BaseObject() as IDictionary;
                        if (asDict != null)
                        {
                            foreach (var key in asDict.Keys)
                            {
                                SetItemPropertyValue(path, item, key.ToString(), asDict[key]);
                            }
                        }
                        else
                        {
                            foreach (PSPropertyInfo property in propertyToSet.Properties)
                            {
                                SetItemPropertyValue(path, item, property.Name, property.Value);
                            }
                        }
                        WriteItem(item);
                    });
                }
            }
            catch (Exception ex)
            {
                PowerShellLog.Error($"Error while executing GetProperty(string path='{path}', PSObject propertyValue)", ex);
                throw;
            }
        }

        private Item GetDynamicItem(string path)
        {
            Item item;
            if (!TryGetDynamicParam(ItemParam, out item))
            {
                item = GetItemInternal(path, true).FirstOrDefault();
            }
            return item;
        }

        private void SetItemPropertyValue(string path, Item item, string propertyName, object propertyValue)
        {
            if (ShouldProcess(path, $"Setting property '{propertyName}' to '{propertyValue}'"))
            {
                if (item.Fields?[propertyName] != null)
                {
                    ItemShellExtensions.ModifyProperty(item, propertyName, propertyValue);
                }
                else
                {
                    WriteWarning($"Property name ’{propertyName}’ doesn’t exist for item at path ’{path}’");
                }
            }
        }

        public void ClearProperty(string path, Collection<string> propertyToClear)
        {
            try
            {
                LogInfo("Executing ClearProperty(string path='{0}', string propertyToClear='{1}')",
                    path,
                    propertyToClear.Aggregate((seed, curr) => seed + ',' + curr));
                var item = GetDynamicItem(path);
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