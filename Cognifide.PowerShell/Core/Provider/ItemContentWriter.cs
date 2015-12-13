using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Microsoft.PowerShell.Commands;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;

namespace Cognifide.PowerShell.Core.Provider
{
    internal class ItemContentWriter : ItemContentReaderWriterBase, IContentWriter
    {
        private enum Mode { Write, Append }

        private Mode mode = Mode.Write;
        private string fieldName = null;

        private Encoder encoder;
        private bool contentCommitted = false;
        private StreamWriter memWriter = null;
        private bool mediaBlob = false;
        private string extension;
        private bool fileBased = false;
        private string path;

        public ItemContentWriter(CmdletProvider provider, Item item, string path,  FileSystemCmdletProviderEncoding encoding, string extension, bool raw, bool fileBased, bool versioned)
			: base(provider, item, encoding, raw)
		{
            if (Encoding != null && !Raw)
            {
                encoder = Encoding.GetEncoder();
            }
            if (!string.IsNullOrEmpty(extension))
            {
                this.extension = extension.StartsWith(".") ? extension : "." + extension;
            }
            this.fileBased = fileBased;
            this.path = path;
		}

        public IList Write(IList content)
        {
            if (Stream == null)
            {
                CreateStreams();
            }

            if (content.Count <= 0)
            {
                return content;
            }

            if (content[0] is PSObject)
            {
                content = content.BaseArray();
            }

            if (content[0] is string)
            {
                if (encoder == null)
                {
                    encoder = Encoding.UTF8.GetEncoder();
                }

                foreach (string str in content)
                {
                    var chars = (str + "\n").ToCharArray();
                    var numBytes = encoder.GetByteCount(chars, 0, chars.Length, false);

                    var bytes = new byte[Math.Min(numBytes, ByteBufferSize)];
                    var convertedChars = 0;

                    var completed = false;
                    while (!completed)
                    {
                        int charsUsed;
                        int bytesUsed;

                        encoder.Convert(chars, convertedChars, chars.Length - convertedChars, bytes, 0, bytes.Length, false, out charsUsed, out bytesUsed, out completed);
                        convertedChars += charsUsed;

                        Stream.Write(bytes, 0, bytesUsed);
                    }
                }
            }
            else if (content[0] is byte)
            {
                var bytes = content as byte[] ?? content.Cast<byte>().ToArray();

                var bytesWritten = 0;
                while (bytesWritten < bytes.Length)
                {
                    var written = Math.Min(bytes.Length - bytesWritten, ByteBufferSize);
                    Stream.Write(bytes, bytesWritten, written);
                    bytesWritten += written;
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(content));
            }

            return content;
        }

        public void Truncate()
        {
            if (Stream == null)
            {
                CreateStreams();
            }

            Stream.SetLength(0);
        }

        public override void Seek(long offset, SeekOrigin origin)
        {
            if (mode == Mode.Write && offset == 0 && origin == SeekOrigin.End)
            {
                mode = Mode.Append;
                return;
            }

            base.Seek(offset, origin);
        }
        private void CommitContent()
        {
            if (!contentCommitted)
            {
                contentCommitted = true;
                if (mediaBlob)
                {
                    var mc = new MediaCreator();
                    var mco = new MediaCreatorOptions()
                    {
                        Database = Item.Database,
                        Language = Item.Language,
                        FileBased = fileBased,
                    };
                    mc.AttachStreamToMediaItem(Stream, Item.Paths.Path, Item.Name + extension, mco);
                }
            }
        }

        public override void Close()
        {
            CommitContent();
            memWriter?.Close();
            base.Close();
        }

        public override void Dispose()
        {
            CommitContent();
            memWriter?.Dispose();
            base.Dispose();

        }

        private void CreateStreams()
        {
            Stream = new MemoryStream();
            if (IsMediaPath(path))
            {
                mediaBlob = true;
                if (mode == Mode.Append && Item != null)
                {
                    MediaItem mediaItem = (MediaItem) Item;
                    Media media = MediaManager.GetMedia(mediaItem);
                    var mediaStream = media.GetStream();
                    Stream.SetLength(mediaStream.Length);
                    mediaStream.CopyTo(Stream);
                    mediaStream.Dispose();
                }
            }
            else
            {
                Stream = new MemoryStream();
                if (mode == Mode.Append)
                {
                    if (Item.TemplateName == TemplateNames.ScriptTemplateName)
                    {
                        memWriter = new StreamWriter(Stream);
                        memWriter.Write(Item[ScriptItemFieldNames.Script]);
                        memWriter.Flush();
                    }
                }
            }
        }
    }    
}
