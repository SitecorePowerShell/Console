using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using Cognifide.PowerShell.Core.Utility;
using Microsoft.PowerShell.Commands;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Version = Sitecore.Data.Version;

namespace Cognifide.PowerShell.Core.Provider
{
    public partial class PsSitecoreItemProvider : IContentCmdletProvider
    {
        public IContentReader GetContentReader(string path)
        {
            var item = GetItemInternal(path).FirstOrDefault();
            if (item== null)
            {
                Exception exception = new IOException($"Cannot find path '{path}' or item because it does not exist.");
                WriteError(new ErrorRecord(exception, "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
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
            paramAdded |= AddDynamicParameter(typeof(Database), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), AmbiguousPathsParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), RawParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(FileSystemCmdletProviderEncoding), EncodingParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), DelimiterParam, ref dic, false, false);

            return paramAdded ? dic : null;
        }

        public IContentWriter GetContentWriter(string path)
        {
            var item = GetItemInternal(path).FirstOrDefault();
            if (item == null && !ItemContentReaderWriterBase.IsMediaPath(path))
            {
                Exception exception = new IOException($"Cannot find path '{path}' or item because it does not exist.");
                WriteError(new ErrorRecord(exception, "ItemDoesNotExist", ErrorCategory.ObjectNotFound, path));
            }
            var extension = GetDynamicParamValue(ExtensionParam, string.Empty);
            var encoding = GetDynamicParamValue(EncodingParam, FileSystemCmdletProviderEncoding.Unknown);
            return new ItemContentWriter(this, item, path, encoding, extension, IsDynamicParamSet(RawParam),
                IsDynamicParamSet(FileBasedParam), IsDynamicParamSet(VersionedParam));
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            LogInfo("Executing GetContentReaderDynamicParameters(string path='{0}')", path);
            var dic = DynamicParameters as RuntimeDefinedParameterDictionary;
            var paramAdded = AddDynamicParameter(typeof(string), LanguageParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), VersionParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), QueryParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), IdParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(Database), DatabaseParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(string), UriParam, ref dic, false, true);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), RawParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(FileSystemCmdletProviderEncoding), EncodingParam, ref dic);
            paramAdded |= AddDynamicParameter(typeof(string), ExtensionParam, ref dic, false, false);
            paramAdded |= AddDynamicParameter(typeof(SwitchParameter), FileBasedParam, ref dic);

            return paramAdded ? dic : null;
        }

        public void ClearContent(string path)
        {
            //throw new NotImplementedException();
        }

        public object ClearContentDynamicParameters(string path)
        {
            throw new NotImplementedException();
        }
    }
}