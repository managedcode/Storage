using ManagedCode.Storage.Core.Helpers;

namespace ManagedCode.Storage.BrowserServerHost;

internal sealed class DeterministicPayloadStream(long length, int seed, long progressIntervalBytes = 0, Action<long>? progressCallback = null) : Stream
{
    private readonly Random _random = new(seed);
    private uint _crc = Crc32Helper.Begin();
    private long _nextProgressReportBytes = progressIntervalBytes > 0 ? progressIntervalBytes : long.MaxValue;

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position { get; set; }

    public bool IsCompleted => Position == Length;

    public uint CompletedCrc => IsCompleted
        ? Crc32Helper.Complete(_crc)
        : throw new InvalidOperationException("CRC is available only after the entire payload has been read.");

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return Read(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        if (buffer.IsEmpty || Position >= Length)
            return 0;

        var bytesToRead = (int)Math.Min(buffer.Length, Length - Position);
        var destination = buffer[..bytesToRead];
        _random.NextBytes(destination);
        _crc = Crc32Helper.Update(_crc, destination);
        Position += bytesToRead;
        ReportProgressIfNeeded(progressIntervalBytes, progressCallback);
        return bytesToRead;
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Read(buffer.Span));
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
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

    private void ReportProgressIfNeeded(long progressInterval, Action<long>? progressHandler)
    {
        if (progressInterval <= 0 || progressHandler is null)
            return;

        while (Position >= _nextProgressReportBytes && _nextProgressReportBytes <= Length)
        {
            progressHandler(_nextProgressReportBytes);
            _nextProgressReportBytes += progressInterval;
        }
    }
}
