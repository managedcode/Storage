using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.CloudKit.Clients;

internal sealed class BorrowedReadStream(Stream source) : Stream
{
    public override bool CanRead => source.CanRead;
    public override bool CanSeek => source.CanSeek;
    public override bool CanWrite => false;
    public override long Length => source.Length;
    public override long Position { get => source.Position; set => source.Position = value; }

    public override int Read(byte[] buffer, int offset, int count) => source.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => source.Read(buffer);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        source.ReadAsync(buffer, offset, count, cancellationToken);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        source.ReadAsync(buffer, cancellationToken);
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        source.CopyToAsync(destination, bufferSize, cancellationToken);
    public override long Seek(long offset, SeekOrigin origin) => source.Seek(offset, origin);

    public override void Flush() { }
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
