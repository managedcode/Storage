using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Browser.Interop;

namespace ManagedCode.Storage.Browser;

internal sealed class BrowserStorageOpfsReadStream(
    BrowserIndexedDbInterop interop,
    string databaseName,
    string blobKey,
    ulong length,
    int chunkSizeBytes,
    int chunkBatchSize) : Stream
{
    private readonly int _windowSizeBytes = checked(chunkSizeBytes * Math.Max(chunkBatchSize, 1));
    private byte[] _currentWindow = [];
    private int _currentWindowOffset;
    private long _currentWindowStart = -1;
    private long _position;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => false;

    public override long Length => (long)length;

    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _position = value;
            ClearWindow();
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return ReadAsync(buffer.AsMemory(offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    public override int Read(Span<byte> buffer)
    {
        var rented = new byte[buffer.Length];
        var bytesRead = Read(rented, 0, rented.Length);
        rented.AsSpan(0, bytesRead).CopyTo(buffer);
        return bytesRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        if (buffer.Length == 0 || Position >= Length)
            return 0;

        var totalRead = 0;

        while (buffer.Length > 0 && Position < Length)
        {
            if (!IsCurrentWindowLoaded(Position))
                await LoadWindowAsync(cancellationToken).ConfigureAwait(false);

            if (_currentWindow.Length == 0)
                break;

            var bytesToCopy = Math.Min(buffer.Length, _currentWindow.Length - _currentWindowOffset);
            _currentWindow.AsMemory(_currentWindowOffset, bytesToCopy).CopyTo(buffer);
            buffer = buffer[bytesToCopy..];
            _currentWindowOffset += bytesToCopy;
            _position += bytesToCopy;
            totalRead += bytesToCopy;
        }

        return totalRead;
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var target = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Position = Math.Clamp(target, 0, Length);
        return Position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        throw new NotSupportedException();
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    private async Task LoadWindowAsync(CancellationToken cancellationToken)
    {
        if (Position >= Length)
        {
            ClearWindow();
            return;
        }

        var startOffset = AlignToChunkBoundary(Position);
        var bytesToRead = (int)Math.Min(_windowSizeBytes, Length - startOffset);
        _currentWindow = await interop.ReadPayloadRangeAsync(databaseName, blobKey, startOffset, bytesToRead, cancellationToken)
            .ConfigureAwait(false) ?? [];
        _currentWindowStart = startOffset;
        _currentWindowOffset = (int)(Position - startOffset);
    }

    private bool IsCurrentWindowLoaded(long position)
    {
        return _currentWindowStart >= 0
               && position >= _currentWindowStart
               && position < _currentWindowStart + _currentWindow.Length;
    }

    private long AlignToChunkBoundary(long position)
    {
        var chunkSize = Math.Max(chunkSizeBytes, 1);
        return (position / chunkSize) * chunkSize;
    }

    private void ClearWindow()
    {
        _currentWindow = [];
        _currentWindowOffset = 0;
        _currentWindowStart = -1;
    }
}
