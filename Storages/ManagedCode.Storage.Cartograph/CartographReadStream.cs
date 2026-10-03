using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Cartograph.Catalog;
using Cartograph.Format;

namespace ManagedCode.Storage.Cartograph;

internal sealed class CartographReadStream(CartographArtifact artifact, CatalogEntry entry) : Stream
{
    private RecordLease? _record;
    private int _recordIndex;
    private long _recordOffset;
    private long _position;
    private bool _disposed;

    public override bool CanRead => !_disposed;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return entry.Length;
        }
    }

    public override long Position
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _position;
        }
        set => throw new NotSupportedException("Catalog streams are sequential and read-only.");
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (buffer.IsEmpty || _position == entry.Length)
            return 0;

        while (NeedsRecord())
            _record = artifact.Artifact.ReadRecord(entry.GlobalIndex + _recordIndex);

        return CopyRecord(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (buffer.IsEmpty || _position == entry.Length)
            return 0;

        while (NeedsRecord())
            _record = await artifact.Artifact.ReadRecordAsync(entry.GlobalIndex + _recordIndex, cancellationToken).ConfigureAwait(false);

        return CopyRecord(buffer.Span);
    }

    private bool NeedsRecord()
    {
        if (_record is null)
            return true;
        if (_recordOffset < _record.Length)
            return false;

        _record.Dispose();
        _record = null;
        _recordOffset = 0;
        _recordIndex++;
        return true;
    }

    private int CopyRecord(Span<byte> buffer)
    {
        var record = _record ?? throw new InvalidOperationException("No active record lease.");
        var count = (int)Math.Min(buffer.Length, record.Length - _recordOffset);
        record.Sequence.Slice(_recordOffset, count).CopyTo(buffer);
        _recordOffset += count;
        _position += count;
        return count;
    }

    public override void Flush() => ObjectDisposedException.ThrowIf(_disposed, this);
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
        {
            _disposed = true;
            _record?.Dispose();
            artifact.Dispose();
        }

        base.Dispose(disposing);
    }
}
