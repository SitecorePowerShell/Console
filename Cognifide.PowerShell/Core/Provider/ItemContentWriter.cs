using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Provider;
using System.Text;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Cognifide.PowerShell.Core.Utility;
using Microsoft.PowerShell.Commands;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Resources.Media;

namespace Cognifide.PowerShell.Core.Provider
{
    internal class ItemContentWriter : ItemContentReaderWriterBase, IContentWriter
    {
        private enum Mode { Write, Append }

        private Mode mode = Mode.Write;

        private Encoder encoder;
        private bool contentCommitted;
        private StreamWriter memWriter;
        private bool mediaBlob;
        private readonly string extension;
        private readonly bool fileBased;
        private readonly string path;
        private readonly bool versioned;
        private readonly string language;
        private readonly string database;

        public ItemContentWriter(CmdletProvider provider, Item item, string path,  FileSystemCmdletProviderEncoding encoding, string extension, bool raw, bool fileBased, bool versioned, string language)
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
            this.versioned = versioned;
            this.language = language ?? Item?.Language?.Name;
            database = Item?.Database?.Name ?? PathUtilities.GetDrive(path, "master");

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

                        Stream?.Write(bytes, 0, bytesUsed);
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
                    Stream?.Write(bytes, bytesWritten, written);
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

            Stream?.SetLength(0);
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
                        Database = Factory.GetDatabase(database),
                        FileBased = fileBased,
                        Versioned = versioned
                    };
                    if (!string.IsNullOrEmpty(language))
                    {
                        mco.Language = LanguageManager.GetLanguage(language);
                    }

                    mc.AttachStreamToMediaItem(Stream, PathUtilities.GetSitecorePath(path), PathUtilities.GetLeafFromPath(path) + extension, mco);
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
                    MediaItem mediaItem = Item;
                    Media media = MediaManager.GetMedia(mediaItem);
                    var mediaStream = media.GetStream();
                    Stream.SetLength(mediaStream.Length);
                    mediaStream.CopyTo(Stream);
                    mediaStream.Dispose();
                }
            }
            else
            {
                if (mode == Mode.Append)
                {
                    if (Item.IsPowerShellScript())
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
