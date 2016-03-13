using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Cognifide.PowerShell.Core.Utility
{
    public class ZipUtils
    {
        private const int ZipLeadBytes = 0x04034b50;

        public static bool IsZipContent(Stream content)
        {
            if (content == null || !content.CanSeek) return false;

            content.Seek(0, 0);

            try
            {
                var bytes = new byte[4];

                content.Read(bytes, 0, 4);

                return (BitConverter.ToInt32(bytes, 0) == ZipLeadBytes);
            }
            finally
            {
                content.Seek(0, 0);  // set the stream back to the begining
            }
        }
    }
}