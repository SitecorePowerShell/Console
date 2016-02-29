using System;
using System.Collections;
using System.IO;
using System.Management.Automation.Provider;
using System.Text;
using Cognifide.PowerShell.Core.Extensions;
using Cognifide.PowerShell.Core.Settings;
using Microsoft.PowerShell.Commands;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Provider
{
    internal class ItemContentReader : ItemContentReaderWriterBase, IContentReader
    {
        private Decoder decoder;

        public ItemContentReader(CmdletProvider provider, Item item, FileSystemCmdletProviderEncoding encoding, bool raw)
			: base(provider, item, encoding, raw)
		{
            if (Encoding != null && !Raw)
            {
                decoder = Encoding.GetDecoder();
            }
        }

        public IList Read(long readCount)
        {
            if (Stream == null)
            {
                CreateStreams();
            }

            if (decoder == null)
            {
                if (Raw)
                {
                    var bytes = ReadRawBytes();
                    return bytes.Length == 0 ? (IList) new byte[] {} : new[] {bytes};
                }

                if (readCount == 1)
                {
                    var b = ReadByte();
                    return b == null ? new byte[0] : new[] { (byte)b };
                }

                return ReadBytes(readCount);
            }

            var result = Raw ? ReadRawString() : ReadString();
            return result == null ? new string[0] : new[] { result };
        }

        private byte[] ReadRawBytes()
        {
            if (Stream.Position < Stream.Length)
            {
                var buffer = new byte[Stream.Length];
                Stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }
            return new byte[0];
        }

        private byte? ReadByte()
        {
            var b = Stream.ReadByte();

            if (b == -1)
            {
                return null;
            }

            return (byte)b;
        }

        private byte[] ReadBytes(long readCount)
        {
            if (readCount <= 0)
            {
                readCount = long.MaxValue;
            }

            if (readCount > ByteBufferSize)
            {
                readCount = ByteBufferSize;
            }

            var buffer = new byte[(int)readCount];

            var read = Stream.Read(buffer, 0, buffer.Length);

            if (read == buffer.Length)
            {
                return buffer;
            }

            var slice = new byte[read];

            Buffer.BlockCopy(buffer, 0, slice, 0, read);

            return slice;
        }

        private string ReadRawString()
        {
            var result = new StringBuilder();

            var buffer = new byte[ByteBufferSize];

            for (;;)
            {
                var read = Stream.Read(buffer, 0, buffer.Length);

                var numChars = decoder.GetCharCount(buffer, 0, read, read == 0);
                if (read == 0 && numChars == 0)
                {
                    break;
                }

                var chars = new char[numChars];
                decoder.GetChars(buffer, 0, read, chars, 0, read == 0);
                result.Append(chars);
            }

            var resultString = result.ToString();

            if (resultString == "")
            {
                return null;
            }

            return resultString;
        }

        private string ReadString()
        {
            var result = new StringBuilder();

            var buffer = new byte[ByteBufferSize];

            for (;;)
            {
                for (var i = 0; i < buffer.Length; i++)
                {
                    var b = Stream.ReadByte();

                    if (b == -1 && i == 0 && result.Length == 0)
                    {
                        return null;
                    }

                    if (b == '\r' || b == '\n' || b == -1)
                    {
                        var numChars = decoder.GetCharCount(buffer, 0, i, true);
                        var chars = new char[numChars];
                        decoder.GetChars(buffer, 0, i, chars, 0, true);
                        result.Append(chars);

                        return result.ToString();
                    }

                    buffer[i] = (byte)b;
                }

                // Ran out of buffer space. Put whatever we got so far into result, leave half-read characters in decoder, and restart reading into the buffer from index 0
                {
                    var numChars = decoder.GetCharCount(buffer, 0, buffer.Length, false);
                    var chars = new char[numChars];
                    decoder.GetChars(buffer, 0, buffer.Length, chars, 0, false);
                    result.Append(chars);
                }
            }
        }

        private void CreateStreams()
        {
            if (Item.IsPowerShellScript())
            {
                Stream = new MemoryStream();
                StreamWriter memWriter = new StreamWriter(Stream);
                memWriter.Write(Item[ScriptItemFieldNames.Script]);
                memWriter.Flush();
                Stream.Position = 0;
            }
            else if (IsMediaPath(Item.Paths.Path))
            {
                MediaItem media = Item;
                if (media != null)
                {
                    Stream = media.GetMediaStream();
                }
            }
        }

    }
}
