using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using Sitecore.Data.Managers;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Shell.Provider
{
    public partial class PsSitecoreItemProvider
    {
        private const string FailSilentlyParam = "FailSilently";
        private const string QueryParam = "Query";
        private const string LanguageParam = "Language";
        private const string VersionParam = "Version";
        private const string StartWorkflowParam = "StartWorkflow";
        private const string PermanentlyParam = "Permanently";

        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
        {
            return null;
        }

        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
        {
            return null;
        }

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
        {
            return null;
        }

        protected static bool AddDynamicParameter(Type type, string name, ref RuntimeDefinedParameterDictionary dic)
        {
            bool paramAdded = false;

            if (dic == null || !dic.ContainsKey(name))
            {
                var attrib = new ParameterAttribute
                    {
                        Mandatory = false,
                        ValueFromPipeline = false
                    };

                var param = new RuntimeDefinedParameter
                    {
                        IsSet = false,
                        Name = name,
                        ParameterType = type
                    };
                param.Attributes.Add(attrib);

                if (dic == null)
                {
                    dic = new RuntimeDefinedParameterDictionary();
                }
                dic.Add(name, param);
                paramAdded = true;
            }

            return paramAdded;
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            LogInfo("Executing RemoveItemDynamicParameters(string path='{0}', string recurse='{1}')", path, recurse);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            bool paramAdded = FailSilentlyDynamicParameters(ref dic);
            paramAdded |= AddDynamicParameter(typeof (SwitchParameter), PermanentlyParam, ref dic);
            return paramAdded ? dic : null;
        }

        protected bool IsDynamicParamSet(string paramName)
        {
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            return dic != null && dic[paramName].IsSet;
        }

        protected static bool FailSilentlyDynamicParameters(ref RuntimeDefinedParameterDictionary dic)
        {
            bool paramAdded = false;
            paramAdded |= AddDynamicParameter(typeof (SwitchParameter), FailSilentlyParam, ref dic);
            return paramAdded;
        }

        private void GetVersionAndLanguageParams(out int version, out string language)
        {
            // language selection
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            language = null;
            if (dic != null && dic[LanguageParam].IsSet)
            {
                string forcedLanguage = dic[LanguageParam].Value.ToString();
                language = forcedLanguage.Contains("*") ? forcedLanguage : LanguageManager.GetLanguage(forcedLanguage).Name;
            }

            version = Version.Latest.Number;
            if (dic != null && dic[VersionParam].IsSet)
            {
                int forcedVersion;
                string versionParam = dic[VersionParam].Value.ToString();
                if (versionParam == "*")
                {
                    version = Int32.MaxValue;
                }
                else if (Int32.TryParse(dic[VersionParam].Value.ToString(), out forcedVersion))
                {
                    version = forcedVersion;
                }
            }
        }

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            LogInfo("Executing GetChildItemsDynamicParameters(string path='{0}', string recurse='{1}')", path, recurse);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;

            bool paramAdded = AddDynamicParameter(typeof (string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof (string), VersionParam, ref dic);

            return paramAdded ? dic : null;
        }

        protected override object GetChildNamesDynamicParameters(string path)
        {
            /* for now we'll just use the same parameters as gci */
            return GetChildItemsDynamicParameters(path, false);
        }

        protected override object GetItemDynamicParameters(string path)
        {
            LogInfo("Executing GetItemDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;

            bool paramAdded = AddDynamicParameter(typeof (string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof (string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof (string), QueryParam, ref dic);

            return paramAdded ? dic : null;
        }


        private static void SignalPathDoesNotExistError(string path)
        {
            throw new ObjectNotFoundException(string.Format("Cannot find path '{0}' because it does not exist.",
                                                            path));
        }

        private void CheckOperationAllowed(string operation, bool isOperationAllowed, string path)
        {
            if (!isOperationAllowed && !IsDynamicParamSet(FailSilentlyParam))
            {
                throw new UnauthorizedAccessException(
                    string.Format("Cannot {0} item {1} - Access to the path is denied.", operation, path));
            }
        }


        protected override object NewItemDynamicParameters(string path, string itemTypeName, object newItemValue)
        {
            LogInfo(
                "Executing NewItemDynamicParameters(string path='{0}', string itemTypeName='{1}', newItemValue='{2}')",
                path, itemTypeName, newItemValue);

            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            bool paramAdded = AddDynamicParameter(typeof (SwitchParameter), StartWorkflowParam, ref dic);
            return paramAdded ? dic : null;
        }
    }
}