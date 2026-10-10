using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

public partial class VirtualFile
{
    /// <inheritdoc />
    public async ValueTask<byte[]> ReadRangeAsync(long offset, int count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        await using var stream = await OpenReadAsync(cancellationToken: cancellationToken);
        if (stream.CanSeek)
        {
            if (offset >= stream.Length)
                return Array.Empty<byte>();
            stream.Seek(offset, SeekOrigin.Begin);
        }
        else if (!await SkipForwardAsync(stream, offset, cancellationToken))
            return Array.Empty<byte>();

        var buffer = new byte[count];
        var bytesRead = await stream.ReadAtLeastAsync(buffer, count, throwOnEndOfStream: false, cancellationToken);
        if (bytesRead < count)
            Array.Resize(ref buffer, bytesRead);
        return buffer;
    }

    private static async Task<bool> SkipForwardAsync(Stream stream, long count, CancellationToken cancellationToken)
    {
        if (count == 0)
            return true;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            while (count > 0)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, (int)Math.Min(count, buffer.Length)), cancellationToken);
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
