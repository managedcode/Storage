using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Streaming;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

/// <summary>
/// Implementation of a virtual file
/// </summary>
public class VirtualFile : IVirtualFile
{
    private readonly IVirtualFileSystem _vfs;
    private readonly IMetadataManager _metadataManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly VfsPath _path;

    private BlobMetadata? _blobMetadata;
    private VfsMetadata? _vfsMetadata;
    private bool _metadataLoaded;

    /// <summary>
    /// Initializes a new instance of VirtualFile
    /// </summary>
    public VirtualFile(
        IVirtualFileSystem vfs,
        IMetadataManager metadataManager,
        IMemoryCache cache,
        ILogger logger,
        VfsPath path)
    {
        _vfs = vfs ?? throw new ArgumentNullException(nameof(vfs));
        _metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _path = path;
    }

    /// <inheritdoc />
    public VfsPath Path => _path;

    /// <inheritdoc />
    public string Name => _path.GetFileName();

    /// <inheritdoc />
    public VfsEntryType Type => VfsEntryType.File;

    /// <inheritdoc />
    public DateTimeOffset CreatedOn => _vfsMetadata?.Created ?? _blobMetadata?.CreatedOn ?? DateTimeOffset.MinValue;

    /// <inheritdoc />
    public DateTimeOffset LastModified => _vfsMetadata?.Modified ?? _blobMetadata?.LastModified ?? DateTimeOffset.MinValue;

    /// <inheritdoc />
    public long Size => (long)(_blobMetadata?.Length ?? 0);

    /// <inheritdoc />
    public string? ContentType => _blobMetadata?.MimeType;

    /// <inheritdoc />
    public string? ETag { get; private set; }

    /// <inheritdoc />
    public string? ContentHash { get; private set; }

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _vfs.FileExistsAsync(_path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing file metadata: {Path}", _path);

        _blobMetadata = await _metadataManager.GetBlobInfoAsync(_path.ToBlobKey(), cancellationToken);
        _vfsMetadata = await _metadataManager.GetVfsMetadataAsync(_path.ToBlobKey(), cancellationToken);
        _metadataLoaded = true;

        if (_blobMetadata != null)
        {
            ETag = _blobMetadata.Uri?.Query.Contains("sv=") == true ?
                ExtractETagFromUri(_blobMetadata.Uri) : null;
        }

        if (_vfs.Options.EnableCache)
        {
            var metadataKey = $"file_metadata:{_vfs.ContainerName}:{_path}";
            var entry = new MetadataCacheEntry
            {
                Metadata = _vfsMetadata ?? new VfsMetadata(),
                CustomMetadata = new Dictionary<string, string>(),
                CachedAt = DateTimeOffset.UtcNow,
                ETag = ETag,
                Size = (long)(_blobMetadata?.Length ?? 0),
                ContentType = _blobMetadata?.MimeType,
                BlobMetadata = _blobMetadata
            };
            _cache.Set(metadataKey, entry, _vfs.Options.CacheTTL);

            var customKey = $"file_custom_metadata:{_vfs.ContainerName}:{_path}";
            _cache.Remove(customKey);
        }
    }

    /// <inheritdoc />
    public async ValueTask<IVirtualDirectory> GetParentAsync(CancellationToken cancellationToken = default)
    {
        var parentPath = _path.GetParent();
        return await _vfs.GetDirectoryAsync(parentPath, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenReadAsync(
        StreamOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new StreamOptions();

        _logger.LogDebug("Opening read stream: {Path}", _path);

        await EnsureMetadataLoadedAsync(cancellationToken);

        if (_blobMetadata == null)
        {
            throw new VfsNotFoundException(_path);
        }

        try
        {
            var result = await _vfs.Storage.GetStreamAsync(_path.ToBlobKey(), cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                throw new VfsOperationException($"Failed to open read stream for file: {_path}");
            }

            return result.Value;
        }
        catch (Exception ex) when (!(ex is VfsException))
        {
            _logger.LogError(ex, "Error opening read stream: {Path}", _path);
            throw new VfsOperationException($"Failed to open read stream for file: {_path}", ex);
        }
    }

    /// <inheritdoc />
    public async Task<Stream> OpenWriteAsync(
        WriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new WriteOptions();

        _logger.LogDebug("Opening write stream: {Path}", _path);

        if (!options.Overwrite && await ExistsAsync(cancellationToken))
        {
            throw new VfsAlreadyExistsException(_path);
        }

        if (!string.IsNullOrEmpty(options.ExpectedETag))
        {
            await EnsureMetadataLoadedAsync(cancellationToken);
            if (ETag != options.ExpectedETag)
            {
                throw new VfsConcurrencyException(
                    "File was modified by another process",
                    _path,
                    options.ExpectedETag,
                    ETag);
            }
        }

        // Return a file-backed write stream that uploads on dispose to avoid buffering large payloads in memory.
        return new VfsWriteStream(_vfs.Storage, _path.ToBlobKey(), options, _cache, _vfs.Options, _logger);
    }

    /// <inheritdoc />
    public async ValueTask<byte[]> ReadRangeAsync(
        long offset,
        int count,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading range: {Path}, offset: {Offset}, count: {Count}", _path, offset, count);

        await using var stream = await OpenReadAsync(
            new StreamOptions { RangeStart = offset, RangeEnd = offset + count - 1 },
            cancellationToken);

        var buffer = new byte[count];
        var bytesRead = await stream.ReadAsync(buffer, 0, count, cancellationToken);

        if (bytesRead < count)
        {
            Array.Resize(ref buffer, bytesRead);
        }

        return buffer;
    }

    /// <inheritdoc />
    public async Task<byte[]> ReadAllBytesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Reading all bytes: {Path}", _path);

        await EnsureMetadataLoadedAsync(cancellationToken);

        if (_blobMetadata == null)
        {
            throw new VfsNotFoundException(_path);
        }

        var result = await _vfs.Storage.DownloadAsync(_path.ToBlobKey(), cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            throw new VfsOperationException($"Failed to read all bytes for file: {_path}");
        }

        await using var localFile = result.Value;
        return await localFile.ReadAllBytesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ReadAllTextAsync(
        Encoding? encoding = null,
        CancellationToken cancellationToken = default)
    {
        encoding ??= Encoding.UTF8;

        _logger.LogDebug("Reading all text: {Path}", _path);

        var bytes = await ReadAllBytesAsync(cancellationToken);
        return encoding.GetString(bytes);
    }

    /// <inheritdoc />
    public async Task WriteAllBytesAsync(
        byte[] bytes,
        WriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Writing all bytes: {Path}, size: {Size}", _path, bytes.Length);

        await using var stream = await OpenWriteAsync(options, cancellationToken);
        await stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
    }

    /// <inheritdoc />
    public async Task WriteAllTextAsync(
        string text,
        Encoding? encoding = null,
        WriteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        encoding ??= Encoding.UTF8;

        _logger.LogDebug("Writing all text: {Path}, length: {Length}", _path, text.Length);

        var bytes = encoding.GetBytes(text);
        await WriteAllBytesAsync(bytes, options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<IReadOnlyDictionary<string, string>> GetMetadataAsync(
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"file_custom_metadata:{_vfs.ContainerName}:{_path}";

        if (_vfs.Options.EnableCache
            && _cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, string>? cached)
            && cached is not null)
        {
            _logger.LogDebug("File metadata (cached): {Path}", _path);
            return cached;
        }

        var metadata = await _metadataManager.GetCustomMetadataAsync(_path.ToBlobKey(), cancellationToken);

        if (_vfs.Options.EnableCache)
        {
            _cache.Set(cacheKey, metadata, _vfs.Options.CacheTTL);
            var metadataKey = $"file_metadata:{_vfs.ContainerName}:{_path}";
            if (_cache.TryGetValue(metadataKey, out MetadataCacheEntry? entry) && entry is not null)
            {
                entry.CustomMetadata = metadata;
                _cache.Set(metadataKey, entry, _vfs.Options.CacheTTL);
            }
        }

        _logger.LogDebug("File metadata: {Path}, count: {Count}", _path, metadata.Count);
        return metadata;
    }

    /// <inheritdoc />
    public async Task SetMetadataAsync(
        IDictionary<string, string> metadata,
        string? expectedETag = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting metadata: {Path}, count: {Count}", _path, metadata.Count);

        if (!string.IsNullOrEmpty(expectedETag))
        {
            await EnsureMetadataLoadedAsync(cancellationToken);
            if (ETag != expectedETag)
            {
                throw new VfsConcurrencyException(
                    "File was modified by another process",
                    _path,
                    expectedETag,
                    ETag);
            }
        }

        var vfsMetadata = _vfsMetadata ?? new VfsMetadata();
        vfsMetadata.Modified = DateTimeOffset.UtcNow;

        await _metadataManager.SetVfsMetadataAsync(
            _path.ToBlobKey(),
            vfsMetadata,
            metadata,
            expectedETag,
            cancellationToken);

        // Invalidate cache
        if (_vfs.Options.EnableCache)
        {
            var metadataKey = $"file_metadata:{_vfs.ContainerName}:{_path}";
            _cache.Remove(metadataKey);
            var customKey = $"file_custom_metadata:{_vfs.ContainerName}:{_path}";
            _cache.Remove(customKey);
        }
    }

    /// <inheritdoc />
    public async Task<IMultipartUpload> StartMultipartUploadAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Starting multipart upload: {Path}", _path);

        // This is a simplified implementation - real multipart upload would depend on the storage provider
        throw new VfsNotSupportedException("Multipart upload", "Not yet implemented in this version");
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(CancellationToken cancellationToken = default)
    {
        return await _vfs.DeleteFileAsync(_path, cancellationToken);
    }

    private async Task EnsureMetadataLoadedAsync(CancellationToken cancellationToken)
    {
        if (_metadataLoaded)
        {
            return;
        }

        if (_vfs.Options.EnableCache)
        {
            var metadataKey = $"file_metadata:{_vfs.ContainerName}:{_path}";
            if (_cache.TryGetValue(metadataKey, out MetadataCacheEntry? entry) && entry is not null)
            {
                _vfsMetadata = entry.Metadata;
                _blobMetadata = entry.BlobMetadata;
                ETag = entry.ETag;
                _metadataLoaded = true;
                return;
            }
        }

        await RefreshAsync(cancellationToken);
    }

    private static string? ExtractETagFromUri(Uri uri)
    {
        // This is a simplified ETag extraction - real implementation would depend on the storage provider
        return null;
    }
}
