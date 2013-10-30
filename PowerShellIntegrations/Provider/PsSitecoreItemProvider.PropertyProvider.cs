using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Cognifide.PowerShell.PowerShellIntegrations.Provider
{
    public partial class PsSitecoreItemProvider
    {
        public void GetProperty(string path, Collection<string> providerSpecificPickList)
        {
            try
            {
                LogInfo("Executing GetProperty(string path='{0}', Collection<string> providerSpecificPickList)", path);

                Item item = GetItemForPath(path);
                if (item != null)
                {
                    // create PSObject from the FileSystemInfo instance
                    PSObject psobj = PSObject.AsPSObject(item);

                    // create the PSObject to copy properties into and that we will return
                    var result = new PSObject();

                    foreach (string name in providerSpecificPickList)
                    {
                        // Copy all the properties from the original object into ’result’
                        PSPropertyInfo prop = psobj.Properties[name];
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

                    foreach (string name in providerSpecificPickList)
                    {
                        Field field = item.Fields[name];
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

        public void SetProperty(string path, PSObject propertyValue)
        {
            try
            {
                LogInfo("Executing SetProperty(string path='{0}', PSObject propertyValue='{1}')", path, propertyValue);
                Item item = GetItemForPath(path);
                if (item != null)
                {
                    string name = propertyValue.Members["Name"].ToString();
                    // create PSObject from the FileSystemInfo instance
                    PSObject psobj = PSObject.AsPSObject(item);

                    // create the PSObject to copy properties into and that we will return
                    var result = new PSObject();

                    // Copy all the properties from the original object into ’result’
                    PSPropertyInfo prop = psobj.Properties[name];
                    object value = null;

                    if (prop != null)
                    {
                        prop.Value = value;
                    }
                    else
                    {
                        Field pageProp = item.Fields[name];
                        if (pageProp != null)
                        {
                            result.Properties[name].Value = value;
                        }
                        WriteWarning(String.Format("Property name ’{0}’ doesn’t exist for item at path ’{1}’", name,
                                                   path));
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
                Item item = GetItemForPath(path);
                item.Edit(args =>
                    {
                        foreach (string property in propertyToClear)
                        {
                            item.Fields[property].Reset();
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