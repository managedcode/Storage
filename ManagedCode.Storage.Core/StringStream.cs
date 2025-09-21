using System;
using System.IO;

namespace ManagedCode.Storage.Core
{
    public class StringStream(string str) : Stream
    {
        private readonly string _string = str ?? throw new ArgumentNullException(nameof(str));

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => _string.Length * 2;
        public override long Position { get; set; }

        private byte this[int i] => (byte)(_string[i / 2] >> ((i & 1) * 8));

        public override long Seek(long offset, SeekOrigin origin)
        {
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => Length + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
            };

            return Position;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var length = Math.Min(count, Length - Position);
            for (var i = 0; i < length; i++)
                buffer[offset + i] = this[(int)Position++];

            return (int)length;
        }

        public override int ReadByte() => Position < Length ? this[(int)Position++] : -1;

        public override void Flush() { }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override string ToString() => _string;
    }
}