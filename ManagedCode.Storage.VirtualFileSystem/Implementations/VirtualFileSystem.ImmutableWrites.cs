using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core.Primitives;
using ManagedCode.Storage.VirtualFileSystem.Core;

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
        InvalidateImmutableWriteCaches(path);
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
        InvalidateImmutableWriteCaches(path);
        return result;
    }

    private void InvalidateImmutableWriteCaches(VfsPath path)
    {
        if (!_options.EnableCache)
        {
            return;
        }

        _cache.Remove($"file_exists:{ContainerName}:{path}");
        _cache.Remove($"file_metadata:{ContainerName}:{path}");
        _cache.Remove($"file_custom_metadata:{ContainerName}:{path}");
    }
}
