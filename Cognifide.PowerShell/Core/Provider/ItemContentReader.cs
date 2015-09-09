using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation.Provider;
using System.Text;
using System.Threading.Tasks;
using Cognifide.PowerShell.Core.Settings;
using Sitecore.Data.Items;

namespace Cognifide.PowerShell.Core.Provider
{
    class ItemContentReader : IContentReader
    {
        private Item item;
        private Stream stream;
        private StreamReader reader;


        public ItemContentReader(Item item)
        {

            this.item = item;
            if (item.TemplateName == TemplateNames.ScriptTemplateName)
            {
                stream = new MemoryStream();
                StreamWriter writer = new StreamWriter(stream);
                writer.Write(item[ScriptItemFieldNames.Script]);
                writer.Flush();
                stream.Position = 0;
            }
            else if(IsMediaItem(item))
            {
                MediaItem media = (MediaItem) item;
                if (media != null)
                {
                    stream = media.GetMediaStream();
                }
            }
            if (stream != null)
            {
                reader = new StreamReader(stream);
            }
        }

        
        private bool IsMediaItem(Item item)
        {
            return item.Paths.IsMediaItem;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void Dispose(bool isDisposing)
        {
            if (!isDisposing)
                return;
            if (reader != null)
            {
                var _reader = reader;
                reader = null;
                stream = null;
                _reader.Close();
            }
        }

        public IList Read(long readCount)
        {
            ArrayList blocks = new ArrayList();
            bool flag = readCount <= 0;
            if (stream != null)
            {
                for (long index = 0; index < readCount || flag; ++index)
                {
                    ReadByLine(blocks);
                }
            }
            return blocks.ToArray();
        }

        public void Seek(long offset, SeekOrigin origin)
        {
            stream?.Seek(offset, origin);
            reader?.DiscardBufferedData();
        }

        public void Close()
        {
            Dispose();
        }

        private bool ReadByLine(ArrayList blocks)
        {
            string str = reader.ReadLine();
            if (str != null)
                blocks.Add(str);
            return reader.Peek() != -1;
        }

    }
}
