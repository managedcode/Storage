using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Browser.Interop;
using ManagedCode.Storage.Browser.Models;
using ManagedCode.Storage.Browser.Options;

namespace ManagedCode.Storage.Browser;

internal sealed class BrowserChunkUploadSession(BrowserIndexedDbInterop interop, BrowserStorageOptions options)
{
    public async Task<BrowserPayloadWriteResult> WriteAsync(Stream stream, string databaseName, string blobKey, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(blobKey);

        var started = await interop.BeginPayloadWriteAsync(databaseName, blobKey, cancellationToken).ConfigureAwait(false);
        if (!started)
        {
            throw new NotSupportedException(
                "ManagedCode.Storage.Browser requires Origin Private File System (OPFS) support. This browser does not expose the required OPFS APIs.");
        }

        return await WriteToOpfsAsync(stream, databaseName, blobKey, cancellationToken).ConfigureAwait(false);
    }

    private static byte[] CopyChunk(byte[] source, int length)
    {
        var chunk = GC.AllocateUninitializedArray<byte>(length);
        Buffer.BlockCopy(source, 0, chunk, 0, length);
        return chunk;
    }

    private async Task<BrowserPayloadWriteResult> WriteToOpfsAsync(Stream stream, string databaseName, string blobKey,
        CancellationToken cancellationToken)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(options.ChunkSizeBytes);
        var batch = new List<BrowserChunkWriteRequest>(options.ChunkBatchSize);
        long length = 0;

        try
        {
            ResetStream(stream);

            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                batch.Add(new BrowserChunkWriteRequest { Data = CopyChunk(buffer, bytesRead) });
                length += bytesRead;

                if (batch.Count == options.ChunkBatchSize)
                    await FlushOpfsBatchAsync(databaseName, blobKey, batch, cancellationToken).ConfigureAwait(false);
            }

            await FlushOpfsBatchAsync(databaseName, blobKey, batch, cancellationToken).ConfigureAwait(false);
            await interop.CompletePayloadWriteAsync(databaseName, blobKey, cancellationToken).ConfigureAwait(false);
            return CreateWriteResult(length, BrowserPayloadStores.Opfs);
        }
        catch
        {
            await interop.AbortPayloadWriteAsync(databaseName, blobKey, cancellationToken).ConfigureAwait(false);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private async Task FlushOpfsBatchAsync(string databaseName, string blobKey, List<BrowserChunkWriteRequest> batch,
        CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
            return;

        await interop.AppendPayloadChunksAsync(databaseName, blobKey, [.. batch], cancellationToken).ConfigureAwait(false);
        batch.Clear();
    }

    private static void ResetStream(Stream stream)
    {
        if (stream.CanSeek)
            stream.Position = 0;
    }

    private static BrowserPayloadWriteResult CreateWriteResult(long length, string payloadStore)
    {
        return new BrowserPayloadWriteResult
        {
            Length = length,
            PayloadStore = payloadStore
        };
    }
}
