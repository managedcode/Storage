using System;
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

        if (_options.EnableCache)
        {
            _cache.Remove($"file_exists:{ContainerName}:{path}");
            _cache.Remove($"file_metadata:{ContainerName}:{path}");
            _cache.Remove($"file_custom_metadata:{ContainerName}:{path}");
        }

        return result;
    }
}
