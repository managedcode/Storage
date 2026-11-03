using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

public partial class VirtualFile
{
    private const int RangeBufferSize = 64 * 1024;

    /// <inheritdoc />
    public async ValueTask<byte[]> ReadRangeAsync(long offset, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        await using var stream = await OpenReadAsync(cancellationToken: cancellationToken);
        if (count == 0)
            return Array.Empty<byte>();

        if (stream.CanSeek)
        {
            var available = stream.Length - offset;
            if (available <= 0)
                return Array.Empty<byte>();

            stream.Seek(offset, SeekOrigin.Begin);
            return await ReadAvailableAsync(stream, (int)Math.Min(count, available), cancellationToken);
        }

        if (!await SkipForwardAsync(stream, offset, cancellationToken))
            return Array.Empty<byte>();

        return count <= RangeBufferSize
            ? await ReadAvailableAsync(stream, count, cancellationToken)
            : await ReadBufferedRangeAsync(stream, count, cancellationToken);
    }

    private static async ValueTask<byte[]> ReadAvailableAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = new byte[count];
        var bytesRead = await stream.ReadAtLeastAsync(buffer, count, throwOnEndOfStream: false, cancellationToken);
        if (bytesRead < count)
            Array.Resize(ref buffer, bytesRead);
        return buffer;
    }

    private static async ValueTask<byte[]> ReadBufferedRangeAsync(Stream stream, int count, CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(RangeBufferSize);
        try
        {
            using var result = new MemoryStream();
            while (count > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, Math.Min(count, RangeBufferSize)), cancellationToken);
                if (read == 0)
                    break;

                result.Write(buffer, 0, read);
                count -= read;
            }

            return result.ToArray();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static async Task<bool> SkipForwardAsync(Stream stream, long count, CancellationToken cancellationToken)
    {
        if (count == 0)
            return true;
        var bufferSize = (int)Math.Min(count, RangeBufferSize);
        var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        try
        {
            while (count > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(count, bufferSize)), cancellationToken);
                if (read == 0)
                    return false;
                count -= read;
            }
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
