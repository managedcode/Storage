using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ManagedCode.Storage.Core.Primitives;

/// <summary>
/// In-memory object writes for payloads the caller already holds. Array-backed memory is written in place;
/// only memory without a backing array is copied once.
/// </summary>
public static class ObjectStorageBytes
{
    /// <summary>Writes an in-memory object with the same conditions and metadata as the stream write.</summary>
    public static async Task<StorageObjectInfo> WriteObjectAsync(
        this IObjectStorage storage,
        string path,
        ReadOnlyMemory<byte> content,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        using var source = OpenRead(content);
        return await storage.WriteObjectAsync(path, source, options, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stages one in-memory part of a multipart object.</summary>
    public static async Task StagePartAsync(
        this IMultipartObjectStorage storage,
        string path,
        string partId,
        ReadOnlyMemory<byte> content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(storage);
        using var source = OpenRead(content);
        await storage.StagePartAsync(path, partId, source, cancellationToken).ConfigureAwait(false);
    }

    internal static MemoryStream OpenRead(ReadOnlyMemory<byte> content) =>
        MemoryMarshal.TryGetArray(content, out var segment) && segment.Array is not null
            ? new MemoryStream(segment.Array, segment.Offset, segment.Count, writable: false)
            : new MemoryStream(content.ToArray(), writable: false);
}
