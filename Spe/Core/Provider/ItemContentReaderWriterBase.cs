using System.IO;
using System.Management.Automation.Provider;
using System.Text;
using Microsoft.PowerShell.Commands;
using Sitecore.Data.Items;
using Spe.Core.Settings;
using Spe.Core.Utility;

namespace Spe.Core.Provider
{
    internal abstract class ItemContentReaderWriterBase
    {
        protected const int ByteBufferSize = 4096;
        protected Item Item { get; }
        protected Encoding Encoding { get; }
        protected Stream Stream { get; set; }
        protected CmdletProvider Provider { get; }
        protected bool Raw { get; }

        protected ItemContentReaderWriterBase(CmdletProvider provider, Item item,
            FileSystemCmdletProviderEncoding encoding, bool raw)
        {
            Item = item;
            Provider = provider;
            Raw = raw;

            if (encoding == FileSystemCmdletProviderEncoding.Unknown)
            {
                encoding = FileSystemCmdletProviderEncoding.Byte;
            }
            Encoding = new FileSystemContentWriterDynamicParameters() {Encoding = encoding}.EncodingType;
        }

        public virtual void Seek(long offset, SeekOrigin origin)
        {
            Stream.Seek(offset, origin);
        }

        public virtual void Close()
        {
            Stream?.Close();
        }

        public virtual void Dispose()
        {
            Stream?.Dispose();
        }
        public static bool IsMediaPath(string path)
        {
            path = PathUtilities.GetSitecorePath(path);
            return path.StartsWith(ApplicationSettings.MediaLibraryPath, System.StringComparison.OrdinalIgnoreCase);
        }


    }
}
