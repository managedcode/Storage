using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Helpers;
using ManagedCode.Storage.Server.Models;

namespace ManagedCode.Storage.Server.ChunkUpload;

internal static class ChunkUploadFiles
{
    private const int CopyBufferSize = 64 * 1024;

    internal static async Task StageChunkAsync(ChunkUploadSession session, FileUploadPayload payload, long length,
        CancellationToken cancellationToken)
    {
        var chunkIndex = payload.Payload.ChunkIndex;
        var chunkFilePath = Path.Combine(session.WorkingDirectory, $"{chunkIndex:D6}.part");
        var stagingPath = chunkFilePath + ".incoming";
        try
        {
            await using (var target = new FileStream(stagingPath, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true))
            await using (var source = payload.File.OpenReadStream())
            {
                await source.CopyToAsync(target, CopyBufferSize, cancellationToken).ConfigureAwait(false);
                if (target.Position != length)
                    throw new InvalidDataException("The staged chunk differs from its form file length.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            File.Move(stagingPath, chunkFilePath, overwrite: true);
            session.RegisterChunk(chunkIndex, chunkFilePath, length);
        }
        finally
        {
            File.Delete(stagingPath);
        }
    }

    internal static async Task<uint> MergeChunksAsync(string destinationFile, IReadOnlyCollection<string> chunkFiles,
        long? expectedLength, CancellationToken cancellationToken)
    {
        await using var destination = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: true);
        using var owner = MemoryPool<byte>.Shared.Rent(CopyBufferSize);
        var buffer = owner.Memory[..CopyBufferSize];
        var crc = Crc32Helper.Begin();
        long length = 0;
        foreach (var chunk in chunkFiles)
        {
            await using var source = new FileStream(chunk, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync: true);
            int read;
            while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                length = checked(length + read);
                if (length > expectedLength)
                    throw new InvalidDataException("The merged file exceeds its declared length.");

                crc = Crc32Helper.Update(crc, buffer.Span[..read]);
                await destination.WriteAsync(buffer[..read], cancellationToken).ConfigureAwait(false);
            }
        }

        if (expectedLength.HasValue && length != expectedLength.Value)
            throw new InvalidDataException("The merged file is shorter than its declared length.");

        return Crc32Helper.Complete(crc);
    }
}
