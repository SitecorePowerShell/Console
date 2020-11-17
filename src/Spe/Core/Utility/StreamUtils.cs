using System.IO;

namespace Spe.Core.Utility
{
    public static class StreamUtils
    {
        public static void CopyStream(Stream from, Stream to, int bufferSize)
        {
            var numArray = new byte[bufferSize];
            for (var i = from.Read(numArray, 0, bufferSize); i > 0; i = from.Read(numArray, 0, bufferSize))
            {
                to.Write(numArray, 0, i);
            }
        }
    }
}