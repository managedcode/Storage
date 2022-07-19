using System;
using System.IO;
using System.Text;

namespace ManagedCode.Storage.Core;

/// <summary>
/// Convert string to byte stream.
/// <para>
/// Slower than <see cref="Encoding.GetBytes()"/>, but saves memory for a large string.
/// </para>
/// </summary>
public class StringStream : Stream
{
    private string input;
    private readonly Encoding encoding;
    private int maxBytesPerChar;
    private int inputLength;
    private int inputPosition;
    private readonly long length;
    private long position;

    public StringStream(string input)
        : this(input, Encoding.UTF8)
    { }

    public StringStream(string input, Encoding encoding)
    {
        this.encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
        this.input = input;
        inputLength = input == null ? 0 : input.Length;
        if (!string.IsNullOrEmpty(input))
            length = encoding.GetByteCount(input);
            maxBytesPerChar = encoding == Encoding.ASCII ? 1 : encoding.GetMaxByteCount(1);
    }

    public override bool CanRead => true;

    public override bool CanSeek => false;

    public override bool CanWrite => false;

    public override long Length => length;

    public override long Position
    {
        get => position;
        set => throw new NotImplementedException();
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (inputPosition >= inputLength)
            return 0;
        if (count < maxBytesPerChar)
            throw new ArgumentException("count has to be greater or equal to max encoding byte count per char");
        int charCount = Math.Min(inputLength - inputPosition, count / maxBytesPerChar);
        int byteCount = encoding.GetBytes(input, inputPosition, charCount, buffer, offset);
        inputPosition += charCount;
        position += byteCount;
        return byteCount;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }
}


/*
public static partial class TextExtensions
{
    public static Encoding PlatformCompatibleUnicode => BitConverter.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
    static bool IsPlatformCompatibleUnicode(this Encoding encoding) => BitConverter.IsLittleEndian ? encoding.CodePage == 1200 : encoding.CodePage == 1201;
    
    public static Stream AsStream(this string @string, Encoding encoding = default) => 
        (@string ?? throw new ArgumentNullException(nameof(@string))).AsMemory().AsStream(encoding);
    public static Stream AsStream(this ReadOnlyMemory<char> charBuffer, Encoding encoding = default) =>
        ((encoding ??= Encoding.UTF8).IsPlatformCompatibleUnicode())
            ? new UnicodeStream(charBuffer)
            : Encoding.CreateTranscodingStream(new UnicodeStream(charBuffer), PlatformCompatibleUnicode, encoding, false);
}

sealed class UnicodeStream : Stream
{
    const int BytesPerChar = 2;

    // By sealing UnicodeStream we avoid a lot of the complexity of MemoryStream.
    ReadOnlyMemory<char> charMemory;
    int position = 0;
    Task<int> _cachedResultTask; // For async reads, avoid allocating a Task.FromResult<int>(nRead) every time we read.

    public UnicodeStream(string @string) : this((@string ?? throw new ArgumentNullException(nameof(@string))).AsMemory()) { }
    public UnicodeStream(ReadOnlyMemory<char> charMemory) => this.charMemory = charMemory;

    public override int Read(Span<byte> buffer)
    {
        EnsureOpen();
        var charPosition = position / BytesPerChar;
        // MemoryMarshal.AsBytes will throw on strings longer than int.MaxValue / 2, so only slice what we need. 
        var byteSlice = MemoryMarshal.AsBytes(charMemory.Slice(charPosition, Math.Min(charMemory.Length - charPosition, 1 + buffer.Length / BytesPerChar)).Span);
        var slicePosition = position % BytesPerChar;
        var nRead = Math.Min(buffer.Length, byteSlice.Length - slicePosition);
        byteSlice.Slice(slicePosition, nRead).CopyTo(buffer);
        position += nRead;
        return nRead;
    }

    public override int Read(byte[] buffer, int offset, int count) 
    {
        ValidateBufferArgs(buffer, offset, count);
        return Read(buffer.AsSpan(offset, count));
    }

    public override int ReadByte()
    {
        // Could be optimized.
        Span<byte> span = stackalloc byte[1];
        return Read(span) == 0 ? -1 : span[0];
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        EnsureOpen();
        if (cancellationToken.IsCancellationRequested) 
            return ValueTask.FromCanceled<int>(cancellationToken);
        try
        {
            return new ValueTask<int>(Read(buffer.Span));
        }
        catch (Exception exception)
        {
            return ValueTask.FromException<int>(exception);
        }   
    }
    
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ValidateBufferArgs(buffer, offset, count);
        var valueTask = ReadAsync(buffer.AsMemory(offset, count));
        if (!valueTask.IsCompletedSuccessfully)
            return valueTask.AsTask();
        var lastResultTask = _cachedResultTask;
        return (lastResultTask != null && lastResultTask.Result == valueTask.Result) ? lastResultTask : (_cachedResultTask = Task.FromResult<int>(valueTask.Result));
    }

    void EnsureOpen()
    {
        if (position == -1)
            throw new ObjectDisposedException(GetType().Name);
    }
    
    // https://docs.microsoft.com/en-us/dotnet/api/system.io.stream.flush?view=net-5.0
    // In a class derived from Stream that doesn't support writing, Flush is typically implemented as an empty method to ensure full compatibility with other Stream types since it's valid to flush a read-only stream.
    public override void Flush() { }
    public override Task FlushAsync(CancellationToken cancellationToken) => cancellationToken.IsCancellationRequested ? Task.FromCanceled(cancellationToken) : Task.CompletedTask;
    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) =>  throw new NotSupportedException();
    
    protected override void Dispose(bool disposing)
    {
        try 
        {
            if (disposing) 
            {
                _cachedResultTask = null;
                charMemory = default;
                position = -1;
            }
        }
        finally 
        {
            base.Dispose(disposing);
        }
    }   
    
    static void ValidateBufferArgs(byte[] buffer, int offset, int count)
    {
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));
        if (offset < 0 || count < 0)
            throw new ArgumentOutOfRangeException();
        if (count > buffer.Length - offset)
            throw new ArgumentException();
    }
}   */