using System;
using System.IO;

namespace Spe.sitecore_modules.PowerShell.Services.Processors
{
    // Read-only stream wrapper that throws once cumulative bytes pulled
    // through it exceed the cap. Wrapping GZipStream with this lets the
    // downstream StreamReader.ReadToEnd fail fast on a decompression
    // bomb instead of allocating until the appdomain OOMs.
    internal sealed class LimitedReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _cap;
        private long _read;

        public LimitedReadStream(Stream inner, int cap)
        {
            _inner = inner;
            _cap = cap;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var n = _inner.Read(buffer, offset, count);
            if (n <= 0) return n;
            _read += n;
            if (_read > _cap)
            {
                throw new RequestBodyTooLargeException(_cap);
            }
            return n;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
