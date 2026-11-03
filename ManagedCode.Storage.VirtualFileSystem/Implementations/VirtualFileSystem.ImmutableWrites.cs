using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Metadata;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

public partial class VirtualFileSystem
{
    /// <inheritdoc />
    public async Task<VerifiedObjectUploadResult> WriteBytesIfAbsentOrSameAsync(
        VfsPath path,
        ReadOnlyMemory<byte> content,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var result = await _storage.RequireObjectStorage().WriteBytesIfAbsentOrSameAsync(
            path.ToBlobKey(), content, options, cancellationToken).ConfigureAwait(false);
        VfsCacheKey.InvalidateFile(_cache, this, path);
        return result;
    }

    /// <inheritdoc />
    public async Task<VerifiedObjectUploadResult> WriteIfAbsentOrSameAsync(
        VfsPath path,
        Stream content,
        long expectedLength,
        StorageWriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var result = await _storage.RequireObjectStorage().WriteIfAbsentOrSameAsync(
            path.ToBlobKey(), content, expectedLength, options, cancellationToken).ConfigureAwait(false);
        VfsCacheKey.InvalidateFile(_cache, this, path);
        return result;
    }
}
