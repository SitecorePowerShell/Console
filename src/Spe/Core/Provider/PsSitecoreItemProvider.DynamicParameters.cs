﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Spe.Core.Utility;
using Version = Sitecore.Data.Version;

namespace Spe.Core.Provider
{
    public partial class PsSitecoreItemProvider : IDynamicParameters
    {
        [Flags]
        public enum TransferOptions
        {
            None = 0,
            ChangeId = 1,
            AllowDefaultValues = 2,
            AllowStandardValues = 4
        }

        private const string FailSilentlyParam = "FailSilently";
        private const string QueryParam = "Query";
        private const string LanguageParam = "Language";
        private const string VersionParam = "Version";
        private const string StartWorkflowParam = "StartWorkflow";
        private const string PermanentlyParam = "Permanently";
        private const string ArchiveParam = "Archive";
        private const string ItemParam = "Item";
        private const string DestinationItemParam = "DestinationItem";
        private const string IdParam = "ID";
        private const string ForceIdParam = "ForceId";
        private const string DatabaseParam = "Database";
        private const string UriParam = "Uri";
        private const string ParentParam = "Parent";
        private const string AmbiguousPathsParam = "AmbiguousPaths";
        private const string TransferOptionsParam = "TransferOptions";
        private const string RawParam = "Raw";
        private const string EncodingParam = "Encoding";
        private const string DelimiterParam = "Delimiter";
        private const string ExtensionParam = "Extension";
        private const string FileBasedParam = "FileBased";
        private const string VersionedParam = "Versioned";
        private const string WithParentParam = "WithParent";
        private const string WithMissingLanguagesParam = "WithMissingLanguages";
        private const string DepthParam = "Depth";

        public object GetDynamicParameters()
        {
            return null;
        }

        public object GetPropertyDynamicParameters(string path, Collection<string> providerSpecificPickList)
        {
            LogInfo("Executing SetPropertyDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            return paramAdded ? dic : null;
        }

        public object ClearPropertyDynamicParameters(string path, Collection<string> propertyToClear)
        {
            LogInfo("Executing SetPropertyDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            return paramAdded ? dic : null;
        }

        public object SetPropertyDynamicParameters(string path, PSObject propertyValue)
        {
            LogInfo("Executing SetPropertyDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            return paramAdded ? dic : null;
        }

        protected static bool AddDynamicParameter(Type type, string name, ref RuntimeDefinedParameterDictionary dic)
        {
            return AddDynamicParameter(type, name, ref dic, false, false, string.Empty);
        }

        protected static bool AddDynamicParameter(Type type, string name, ref RuntimeDefinedParameterDictionary dic,
            bool valueFromPipeline)
        {
            return AddDynamicParameter(type, name, ref dic, valueFromPipeline, false, string.Empty);
        }

        protected static bool AddDynamicParameter(Type type, string name, ref RuntimeDefinedParameterDictionary dic,
            bool valueFromPipeline, bool valueFromPipelineByPropertyName)
        {
            return AddDynamicParameter(type, name, ref dic, valueFromPipeline, valueFromPipelineByPropertyName,
                string.Empty);
        }

        protected static bool AddDynamicParameter(Type type, string name, ref RuntimeDefinedParameterDictionary dic,
            bool valueFromPipeline, bool valueFromPipelineByPropertyName, string paramSetName, bool mandatory = false)
        {
            var paramAdded = false;

            if (dic == null || !dic.ContainsKey(name))
            {
                var attrib = new ParameterAttribute
                {
                    Mandatory = mandatory,
                    ValueFromPipeline = valueFromPipeline,
                    ValueFromPipelineByPropertyName = valueFromPipelineByPropertyName
                };
                if (!string.IsNullOrEmpty(paramSetName))
                {
                    attrib.ParameterSetName = paramSetName;
                }

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

        protected override object CopyItemDynamicParameters(string path, string destination, bool recurse)
        {
            LogInfo("Executing CopyItemDynamicParameters(string path='{0}', destination='{1}', string recurse='{2}')",
                path, destination, recurse);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = FailSilentlyDynamicParameters(ref dic);
            var sourceDrive = PathUtilities.GetDrive(path, SessionState.Drive.Current.Name);
            var destinationDrive = PathUtilities.GetDrive(destination, SessionState.Drive.Current.Name);
            if (!string.Equals(sourceDrive, destination, StringComparison.OrdinalIgnoreCase) &&
                Factory.GetDatabase(sourceDrive) != null && Factory.GetDatabase(destinationDrive) != null)
            {
                paramAdded |= AddDynamicParameter(typeof(TransferOptions), TransferOptionsParam, ref dic);
            }
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            paramAdded |= AddDynamicParameter(typeof(Item), DestinationItemParam, ref dic, false);
            return paramAdded ? dic : null;
        }

        protected override object MoveItemDynamicParameters(string path, string destination)
        {
            LogInfo("Executing MoveItemDynamicParameters(string path='{0}', destination='{1}')",
                path, destination);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = FailSilentlyDynamicParameters(ref dic);
            var sourceDrive = PathUtilities.GetDrive(path, SessionState.Drive.Current.Name);
            var destinationDrive = PathUtilities.GetDrive(destination, SessionState.Drive.Current.Name);
            if (!string.Equals(sourceDrive, destination, StringComparison.OrdinalIgnoreCase) &&
                Factory.GetDatabase(sourceDrive) != null && Factory.GetDatabase(destinationDrive) != null)
            {
                paramAdded |= AddDynamicParameter(typeof(TransferOptions), TransferOptionsParam, ref dic);
            }
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            paramAdded |= AddDynamicParameter(typeof(Item), DestinationItemParam, ref dic, false);
            return paramAdded ? dic : null;
        }

        protected override object RemoveItemDynamicParameters(string path, bool recurse)
        {
            LogInfo("Executing RemoveItemDynamicParameters(string path='{0}', string recurse='{1}')", path, recurse);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = FailSilentlyDynamicParameters(ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), ArchiveParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), PermanentlyParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            return paramAdded ? dic : null;
        }

        protected bool IsDynamicParamSet(string paramName)
        {
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            return dic != null && dic.ContainsKey(paramName) && dic[paramName].IsSet;
        }

        protected static bool FailSilentlyDynamicParameters(ref RuntimeDefinedParameterDictionary dic)
        {
            var paramAdded = false;
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), FailSilentlyParam, ref dic);
            return paramAdded;
        }

        private bool TryGetDynamicParam<T>(string paramName, out T result)
        {
            // language selection
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            bool defined = dic != null && dic.ContainsKey(paramName) && dic[paramName].IsSet;
            result = defined ? (T)dic[paramName].Value : default(T);
            return defined;
        }

        private T GetDynamicParamValue<T>(string paramName, T defaultValue)
        {
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            bool defined = (dic != null && dic[paramName].IsSet);
            T result = defined ? (T)dic[paramName].Value : defaultValue;
            return result;
        }


        private void GetVersionAndLanguageParams(out int version, out string[] availableLanguages)
        {
            // language selection
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            availableLanguages = null;
            version = Version.Latest.Number;

            if (dic == null) return;

            if (dic[LanguageParam].IsSet)
            {
                var validLanguages = new List<string>();

                switch (dic[LanguageParam].Value)
                {
                    case string[] languagesFromParameter:
                        foreach (var providedLanguage in languagesFromParameter)
                        {
                            if (providedLanguage == "*")
                            {
                                // Using the wildcard is equivalent to not providing a value.
                                validLanguages.Add(providedLanguage);
                            }
                            else if (providedLanguage.Contains("*"))
                            {
                                // TODO: I don't think this behavior is correct.
                                // Something like "invalid*" should not work.
                                validLanguages.Add(providedLanguage);
                            }
                            else
                            {
                                var language = LanguageManager.GetLanguage(providedLanguage)?.Name;
                                if(string.IsNullOrEmpty(language)) continue;
                                validLanguages.Add(language);
                            }
                        }
                        break;
                    case string languageFromParameter:
                        if (languageFromParameter == "*")
                        {
                            validLanguages.Add(languageFromParameter);
                        }
                        else if (languageFromParameter.Contains("*"))
                        {
                            // TODO: I don't think this behavior is correct.
                            // Something like "invalid*" should not work.
                            validLanguages.Add(languageFromParameter);
                        }
                        else
                        {
                            var language = LanguageManager.GetLanguage(languageFromParameter)?.Name;
                            if (!string.IsNullOrEmpty(language))
                            {
                                validLanguages.Add(language);
                            }
                        }
                        break;
                }

                availableLanguages = validLanguages.ToArray();
            }

            if (dic[VersionParam].IsSet)
            {
                var versionParam = dic[VersionParam].Value.ToString();
                if (versionParam == "*")
                {
                    version = int.MaxValue;
                }
                else if (int.TryParse(versionParam, out var forcedVersion))
                {
                    version = forcedVersion;
                }
            }
        }

        protected override object GetChildItemsDynamicParameters(string path, bool recurse)
        {
            LogInfo("Executing GetChildItemsDynamicParameters(string path='{0}', string recurse='{1}')", path, recurse);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;

            var paramAdded = AddDynamicParameter(typeof(string[]), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), WithMissingLanguagesParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic, true);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), WithParentParam, ref dic, false, false);
            paramAdded = AppendDepthParameterIfNotNativelySupported(paramAdded, ref dic);
            return paramAdded ? dic : null;
        }

        protected virtual bool AppendDepthParameterIfNotNativelySupported(bool paramAdded,
            ref RuntimeDefinedParameterDictionary dic)
        {
            paramAdded |= AddDynamicParameter(typeof(uint), DepthParam, ref dic, false, false);
            return paramAdded;
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

            var paramAdded = AddDynamicParameter(typeof(string[]), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), WithMissingLanguagesParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);

            return paramAdded ? dic : null;
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
            var paramAdded = AddDynamicParameter(typeof(SwitchParameter), StartWorkflowParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ParentParam, ref dic, true);
            paramAdded |= AddDynamicParameter(typeof(string), ForceIdParam, ref dic);
            return paramAdded ? dic : null;
        }
    }
}