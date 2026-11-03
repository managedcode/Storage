using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ManagedCode.Storage.Core
{
    public class StringStream(string str) : Stream
    {
        private readonly string _string = str ?? throw new ArgumentNullException(nameof(str));
        private long _position;

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => false;
        public override long Length => (long)_string.Length * sizeof(char);

        public override long Position
        {
            get => _position;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);
                _position = value;
            }
        }

        private byte this[long i] => (byte)(_string[(int)(i / sizeof(char))] >> (int)((i & 1) * 8));

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
            ValidateBufferArguments(buffer, offset, count);
            var position = Position;
            var length = (int)Math.Min(count, Math.Max(0, Length - position));
            if (length == 0)
                return 0;

            if (BitConverter.IsLittleEndian)
            {
                var byteOffset = (int)(position % sizeof(char));
                var charCount = (int)((length + (long)byteOffset + 1) / sizeof(char));
                var chars = _string.AsSpan((int)(position / sizeof(char)), charCount);
                MemoryMarshal.AsBytes(chars).Slice(byteOffset, length).CopyTo(buffer.AsSpan(offset, length));
            }
            else
            {
                for (var i = 0; i < length; i++)
                    buffer[offset + i] = this[position + i];
            }

            Position = position + length;
            return length;
        }

        public override int ReadByte() => Position < Length ? this[Position++] : -1;

        public override void Flush() { }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override string ToString() => _string;
    }
}
