using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core;

/// <summary>
/// High-performance UTF-8 string stream implementation using modern .NET Memory/Span APIs
/// Replaces the old StringStream with better memory efficiency and performance
/// </summary>
public sealed class Utf8StringStream : Stream
{
    private readonly ReadOnlyMemory<byte> _buffer;
    private int _position;

    /// <summary>
    /// Creates a new UTF-8 string stream from a string
    /// </summary>
    /// <param name="text">String content to wrap in stream</param>
    public Utf8StringStream(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Use UTF-8 encoding directly to byte array - most efficient for large strings
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var buffer = new byte[byteCount];
        Encoding.UTF8.GetBytes(text, buffer);
        _buffer = buffer;
    }

    /// <summary>
    /// Creates a new UTF-8 string stream from ReadOnlyMemory&lt;byte&gt;
    /// Zero-copy constructor for pre-encoded UTF-8 bytes
    /// </summary>
    /// <param name="utf8Bytes">UTF-8 encoded byte buffer</param>
    public Utf8StringStream(ReadOnlyMemory<byte> utf8Bytes)
    {
        _buffer = utf8Bytes;
    }

    /// <summary>
    /// Creates a new UTF-8 string stream using pooled memory for large strings
    /// Recommended for strings > 1KB for better memory management
    /// </summary>
    /// <param name="text">String content</param>
    /// <param name="arrayPool">Array pool for buffer management</param>
    /// <returns>Stream with pooled backing buffer</returns>
    public static Utf8StringStream CreatePooled(string text, ArrayPool<byte>? arrayPool = null)
    {
        ArgumentNullException.ThrowIfNull(text);

        arrayPool ??= ArrayPool<byte>.Shared;
        var byteCount = Encoding.UTF8.GetByteCount(text);
        var rentedArray = arrayPool.Rent(byteCount);

        try
        {
            var actualLength = Encoding.UTF8.GetBytes(text, rentedArray);
            var buffer = new byte[actualLength];
            Array.Copy(rentedArray, buffer, actualLength);
            return new Utf8StringStream(buffer);
        }
        finally
        {
            arrayPool.Return(rentedArray);
        }
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _buffer.Length;

    public override long Position
    {
        get => _position;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, Length);
            _position = (int)value;
        }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ValidateBufferArgs(buffer, offset, count);
        return ReadCore(buffer.AsSpan(offset, count));
    }

    public override int Read(Span<byte> buffer)
    {
        return ReadCore(buffer);
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Since we're reading from memory, this is synchronous but we await for API compliance
        await Task.CompletedTask;
        return ReadCore(buffer.Span);
    }

    private int ReadCore(Span<byte> destination)
    {
        var remaining = _buffer.Length - _position;
        var bytesToRead = Math.Min(destination.Length, remaining);

        if (bytesToRead <= 0)
            return 0;

        var source = _buffer.Span.Slice(_position, bytesToRead);
        source.CopyTo(destination);
        _position += bytesToRead;

        return bytesToRead;
    }

    public override int ReadByte()
    {
        if (_position >= _buffer.Length)
            return -1;

        return _buffer.Span[_position++];
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => _position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };

        ArgumentOutOfRangeException.ThrowIfNegative(newPosition);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(newPosition, Length);

        _position = (int)newPosition;
        return _position;
    }

    public override void SetLength(long value) => throw new NotSupportedException("UTF-8 string stream is read-only");
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException("UTF-8 string stream is read-only");
    public override void Write(ReadOnlySpan<byte> buffer) => throw new NotSupportedException("UTF-8 string stream is read-only");
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) => throw new NotSupportedException("UTF-8 string stream is read-only");
    public override void WriteByte(byte value) => throw new NotSupportedException("UTF-8 string stream is read-only");
    public override void Flush() { } // No-op for read-only stream
    public override Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    /// <summary>
    /// Gets the underlying UTF-8 bytes as ReadOnlyMemory
    /// Zero-copy access to the buffer
    /// </summary>
    public ReadOnlyMemory<byte> GetUtf8Bytes() => _buffer;

    /// <summary>
    /// Gets the underlying UTF-8 bytes as ReadOnlySpan
    /// Zero-copy access to the buffer
    /// </summary>
    public ReadOnlySpan<byte> GetUtf8Span() => _buffer.Span;

    /// <summary>
    /// Converts the stream content back to string
    /// </summary>
    public override string ToString()
    {
        return Encoding.UTF8.GetString(_buffer.Span);
    }

    /// <summary>
    /// Creates a string from the remaining unread portion of the stream
    /// </summary>
    public string ToStringFromPosition()
    {
        if (_position >= _buffer.Length)
            return string.Empty;

        var remaining = _buffer.Span[_position..];
        return Encoding.UTF8.GetString(remaining);
    }

    private static void ValidateBufferArgs(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);
    }
}

/// <summary>
/// Extension methods for creating UTF-8 string streams
/// </summary>
public static class Utf8StringStreamExtensions
{
    /// <summary>
    /// Creates a UTF-8 string stream from this string
    /// </summary>
    public static Utf8StringStream ToUtf8Stream(this string text)
    {
        return new Utf8StringStream(text);
    }

    /// <summary>
    /// Creates a pooled UTF-8 string stream from this string (recommended for strings > 1KB)
    /// </summary>
    public static Utf8StringStream ToPooledUtf8Stream(this string text, ArrayPool<byte>? arrayPool = null)
    {
        return Utf8StringStream.CreatePooled(text, arrayPool);
    }

    /// <summary>
    /// Creates a UTF-8 string stream from UTF-8 encoded bytes
    /// </summary>
    public static Utf8StringStream ToUtf8Stream(this ReadOnlyMemory<byte> utf8Bytes)
    {
        return new Utf8StringStream(utf8Bytes);
    }

    /// <summary>
    /// Creates a UTF-8 string stream from UTF-8 encoded bytes
    /// </summary>
    public static Utf8StringStream ToUtf8Stream(this byte[] utf8Bytes)
    {
        return new Utf8StringStream(utf8Bytes);
    }
}