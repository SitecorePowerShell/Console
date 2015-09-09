using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Management.Automation;
using System.Management.Automation.Provider;
using Cognifide.PowerShell.Core.Utility;
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
            var item = GetItemForPath(path);
            return new ItemContentReader(item);
        }

        public object GetContentReaderDynamicParameters(string path)
        {
            return null;
        }

        public IContentWriter GetContentWriter(string path)
        {
            throw new NotImplementedException();
        }

        public object GetContentWriterDynamicParameters(string path)
        {
            return null;
        }

        public void ClearContent(string path)
        {
            throw new NotImplementedException();
        }

        public object ClearContentDynamicParameters(string path)
        {
            throw new NotImplementedException();
        }
    }
}