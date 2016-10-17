using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Cognifide.PowerShell.Commandlets;
using Cognifide.PowerShell.Core.Utility;
using Microsoft.PowerShell.Commands;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider : IContentCmdletProvider
    {
        public IContentReader GetContentReader(string path)
        {
            var item = GetItemInternal(path, true).FirstOrDefault();
            if (item== null)
            {
                return null;
            }
            FileSystemCmdletProviderEncoding encoding;
            TryGetDynamicParam(EncodingParam, out encoding);
            string delimiter;
            if (!TryGetDynamicParam(DelimiterParam, out delimiter))
            {
                delimiter = "\n";
            }
            return new ItemContentReader(this, item, encoding, IsDynamicParamSet(RawParam));
        }

        public object GetContentReaderDynamicParameters(string path)
        {
            LogInfo("Executing GetContentReaderDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), RawParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(FileSystemCmdletProviderEncoding), EncodingParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), DelimiterParam, ref dic, false, false);

            return paramAdded ? dic : null;
        }

        public IContentWriter GetContentWriter(string path)
        {
            var item = GetDynamicParamValue(ItemParam, (Item)null);
            if (item == null)
            {
                item = GetItemInternal(path, false).FirstOrDefault();

                if (item == null && !ItemContentReaderWriterBase.IsMediaPath(path))
                {
                    WriteInvalidPathError(path);
                    return null;
                }
            }
            else
            {
                path = item.GetProviderPath();
            }

            var extension = GetDynamicParamValue(ExtensionParam, string.Empty);
            var encoding = GetDynamicParamValue(EncodingParam, FileSystemCmdletProviderEncoding.Unknown);

            string[] language;
            int version;
            GetVersionAndLanguageParams(out version, out language);

            if (language.Length != 1)
            {
                var exception = new IOException($"Cannot Write content to more than 1 language at a time.");
                WriteError(new ErrorRecord(exception, ErrorIds.InvalidOperation.ToString(), ErrorCategory.InvalidArgument,
                    path));
            }
            return new ItemContentWriter(this, item, path, encoding, extension, IsDynamicParamSet(RawParam),
                IsDynamicParamSet(FileBasedParam), IsDynamicParamSet(VersionedParam), language[0]);
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            LogInfo("Executing GetContentReaderDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), RawParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(FileSystemCmdletProviderEncoding), EncodingParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), ExtensionParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), FileBasedParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), VersionedParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(Item), ItemParam, ref dic);

            return paramAdded ? dic : null;
        }

        public void ClearContent(string path)
        {
            // not supported at this moment
        }

        public object ClearContentDynamicParameters(string path)
        {
            //throw new NotImplementedException();
            return null;
        }
    }
}