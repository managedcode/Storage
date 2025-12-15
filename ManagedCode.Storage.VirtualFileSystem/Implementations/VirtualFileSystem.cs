using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

/// <summary>
/// Main implementation of virtual file system
/// </summary>
public class VirtualFileSystem : IVirtualFileSystem
{
    private readonly IStorage _storage;
    private readonly VfsOptions _options;
    private readonly IMetadataManager _metadataManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger<VirtualFileSystem> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of VirtualFileSystem
    /// </summary>
    public VirtualFileSystem(
        IStorage storage,
        IMetadataManager metadataManager,
        IOptions<VfsOptions> options,
        IMemoryCache cache,
        ILogger<VirtualFileSystem> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _metadataManager = metadataManager ?? throw new ArgumentNullException(nameof(metadataManager));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException("options.Value");

        ContainerName = _options.DefaultContainer;
    }

    /// <inheritdoc />
    public IStorage Storage => _storage;

    /// <inheritdoc />
    public string ContainerName { get; }

    /// <inheritdoc />
    public VfsOptions Options => _options;

    /// <inheritdoc />
    public ValueTask<IVirtualFile> GetFileAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting file: {Path}", path);

        return ValueTask.FromResult<IVirtualFile>(new VirtualFile(this, _metadataManager, _cache, _logger, path));
    }

    /// <inheritdoc />
    public async ValueTask<bool> FileExistsAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var cacheKey = $"file_exists:{ContainerName}:{path}";

        if (_options.EnableCache && _cache.TryGetValue(cacheKey, out bool cached))
        {
            _logger.LogDebug("File exists check (cached): {Path} = {Exists}", path, cached);
            return cached;
        }

        try
        {
            var blobInfo = await _metadataManager.GetBlobInfoAsync(path.ToBlobKey(), cancellationToken);
            var exists = blobInfo != null;

            if (_options.EnableCache)
            {
                _cache.Set(cacheKey, exists, _options.CacheTTL);
            }

            _logger.LogDebug("File exists check: {Path} = {Exists}", path, exists);
            return exists;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking file existence: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public async ValueTask<bool> DeleteFileAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Deleting file: {Path}", path);

        try
        {
            var result = await _storage.DeleteAsync(path.ToBlobKey(), cancellationToken);

            if (result.IsSuccess && result.Value)
            {
                if (_options.EnableCache)
                {
                    var existsKey = $"file_exists:{ContainerName}:{path}";
                    _cache.Remove(existsKey);
                    var metadataKey = $"file_metadata:{ContainerName}:{path}";
                    _cache.Remove(metadataKey);
                    var customKey = $"file_custom_metadata:{ContainerName}:{path}";
                    _cache.Remove(customKey);
                }

                _logger.LogDebug("File deleted successfully: {Path}", path);
                return true;
            }

            _logger.LogDebug("File delete failed: {Path}", path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file: {Path}", path);
            throw new VfsOperationException($"Failed to delete file: {path}", ex);
        }
    }

    /// <inheritdoc />
    public ValueTask<IVirtualDirectory> GetDirectoryAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Getting directory: {Path}", path);

        return ValueTask.FromResult<IVirtualDirectory>(new VirtualDirectory(this, _metadataManager, _cache, _logger, path));
    }

    /// <inheritdoc />
    public async ValueTask<bool> DirectoryExistsAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var cacheKey = $"dir_exists:{ContainerName}:{path}";

        if (_options.EnableCache && _cache.TryGetValue(cacheKey, out bool cached))
        {
            _logger.LogDebug("Directory exists check (cached): {Path} = {Exists}", path, cached);
            return cached;
        }

        try
        {
            var prefix = path.ToBlobKey();
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
                prefix += "/";

            // Check if any blobs exist with this prefix
            await foreach (var blob in _storage.GetBlobMetadataListAsync(prefix, cancellationToken))
            {
                if (_options.EnableCache)
                {
                    _cache.Set(cacheKey, true, _options.CacheTTL);
                }

                _logger.LogDebug("Directory exists check: {Path} = true", path);
                return true;
            }

            if (_options.EnableCache)
            {
                _cache.Set(cacheKey, false, _options.CacheTTL);
            }

            _logger.LogDebug("Directory exists check: {Path} = false", path);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking directory existence: {Path}", path);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<DeleteDirectoryResult> DeleteDirectoryAsync(
        VfsPath path,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        _logger.LogDebug("Deleting directory: {Path}, recursive: {Recursive}", path, recursive);

        var result = new DeleteDirectoryResult { Success = true };

        try
        {
            var prefix = path.ToBlobKey();
            if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
                prefix += "/";

            var filesToDelete = new List<string>();

            await foreach (var blob in _storage.GetBlobMetadataListAsync(prefix, cancellationToken))
            {
                // For non-recursive, only delete direct children
                if (!recursive)
                {
                    var relativePath = blob.FullName[prefix.Length..];
                    if (relativePath.Contains('/'))
                    {
                        // This is in a subdirectory, skip it
                        continue;
                    }
                }

                filesToDelete.Add(blob.FullName);
            }

            // Delete files
            foreach (var fileName in filesToDelete)
            {
                try
                {
                    var deleteResult = await _storage.DeleteAsync(fileName, cancellationToken);
                    if (deleteResult.IsSuccess && deleteResult.Value)
                    {
                        result.FilesDeleted++;
                    }
                    else
                    {
                        result.Errors.Add($"Failed to delete file: {fileName}");
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Error deleting file {fileName}: {ex.Message}");
                    _logger.LogWarning(ex, "Error deleting file: {FileName}", fileName);
                }
            }

            // Invalidate cache
            if (_options.EnableCache)
            {
                var cacheKey = $"dir_exists:{ContainerName}:{path}";
                _cache.Remove(cacheKey);
            }

            result.Success = result.Errors.Count == 0;
            _logger.LogDebug("Directory delete completed: {Path}, files deleted: {FilesDeleted}, errors: {ErrorCount}",
                path, result.FilesDeleted, result.Errors.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting directory: {Path}", path);
            result.Success = false;
            result.Errors.Add($"Unexpected error: {ex.Message}");
            return result;
        }
    }

    /// <inheritdoc />
    public async Task MoveAsync(
        VfsPath source,
        VfsPath destination,
        MoveOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= new MoveOptions();

        _logger.LogDebug("Moving: {Source} -> {Destination}", source, destination);

        // For now, implement as copy + delete
        await CopyAsync(source, destination, new CopyOptions
        {
            Overwrite = options.Overwrite,
            PreserveMetadata = options.PreserveMetadata
        }, null, cancellationToken);

        // Delete source
        if (await FileExistsAsync(source, cancellationToken))
        {
            await DeleteFileAsync(source, cancellationToken);
        }
        else if (await DirectoryExistsAsync(source, cancellationToken))
        {
            await DeleteDirectoryAsync(source, true, cancellationToken);
        }

        _logger.LogDebug("Move completed: {Source} -> {Destination}", source, destination);
    }

    /// <inheritdoc />
    public async Task CopyAsync(
        VfsPath source,
        VfsPath destination,
        CopyOptions? options = null,
        IProgress<CopyProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= new CopyOptions();

        _logger.LogDebug("Copying: {Source} -> {Destination}", source, destination);

        // Check if source is a file
        if (await FileExistsAsync(source, cancellationToken))
        {
            await CopyFileAsync(source, destination, options, progress, cancellationToken);
        }
        else if (await DirectoryExistsAsync(source, cancellationToken))
        {
            if (options.Recursive)
            {
                await CopyDirectoryAsync(source, destination, options, progress, cancellationToken);
            }
            else
            {
                throw new VfsOperationException("Source is a directory but recursive copying is disabled");
            }
        }
        else
        {
            throw new VfsNotFoundException(source);
        }

        _logger.LogDebug("Copy completed: {Source} -> {Destination}", source, destination);
    }

    private async Task CopyFileAsync(
        VfsPath source,
        VfsPath destination,
        CopyOptions options,
        IProgress<CopyProgress>? progress,
        CancellationToken cancellationToken)
    {
        var sourceFile = await GetFileAsync(source, cancellationToken);
        var destinationFile = await GetFileAsync(destination, cancellationToken);

        if (await destinationFile.ExistsAsync(cancellationToken) && !options.Overwrite)
        {
            throw new VfsAlreadyExistsException(destination);
        }

        progress?.Report(new CopyProgress
        {
            TotalFiles = 1,
            TotalBytes = sourceFile.Size,
            CurrentFile = source
        });

        // Copy content
        await using var sourceStream = await sourceFile.OpenReadAsync(cancellationToken: cancellationToken);
        await using var destinationStream = await destinationFile.OpenWriteAsync(
            new WriteOptions { Overwrite = options.Overwrite }, cancellationToken);

        await sourceStream.CopyToAsync(destinationStream, cancellationToken);

        // Copy metadata if requested
        if (options.PreserveMetadata)
        {
            var metadata = await sourceFile.GetMetadataAsync(cancellationToken);
            if (metadata.Count > 0)
            {
                var metadataDict = new Dictionary<string, string>(metadata);
                await destinationFile.SetMetadataAsync(metadataDict, cancellationToken: cancellationToken);
            }
        }

        progress?.Report(new CopyProgress
        {
            TotalFiles = 1,
            CopiedFiles = 1,
            TotalBytes = sourceFile.Size,
            CopiedBytes = sourceFile.Size,
            CurrentFile = source
        });
    }

    private async Task CopyDirectoryAsync(
        VfsPath source,
        VfsPath destination,
        CopyOptions options,
        IProgress<CopyProgress>? progress,
        CancellationToken cancellationToken)
    {
        var sourceDir = await GetDirectoryAsync(source, cancellationToken);

        // Calculate total work for progress reporting
        var totalFiles = 0;
        var totalBytes = 0L;

        await foreach (var entry in sourceDir.GetEntriesAsync(recursive: true, cancellationToken: cancellationToken))
        {
            if (entry.Type == VfsEntryType.File && entry is IVirtualFile file)
            {
                totalFiles++;
                totalBytes += file.Size;
            }
        }

        var copiedFiles = 0;
        var copiedBytes = 0L;

        await foreach (var entry in sourceDir.GetEntriesAsync(recursive: true, cancellationToken: cancellationToken))
        {
            if (entry.Type == VfsEntryType.File && entry is IVirtualFile sourceFile)
            {
                var relativePath = entry.Path.Value[source.Value.Length..].TrimStart('/');
                var destPath = destination.Combine(relativePath);
                var destFile = await GetFileAsync(destPath, cancellationToken);

                if (await destFile.ExistsAsync(cancellationToken) && !options.Overwrite)
                {
                    continue; // Skip existing files
                }

                progress?.Report(new CopyProgress
                {
                    TotalFiles = totalFiles,
                    CopiedFiles = copiedFiles,
                    TotalBytes = totalBytes,
                    CopiedBytes = copiedBytes,
                    CurrentFile = entry.Path
                });

                // Copy file content
                await using var sourceStream = await sourceFile.OpenReadAsync(cancellationToken: cancellationToken);
                await using var destStream = await destFile.OpenWriteAsync(
                    new WriteOptions { Overwrite = options.Overwrite }, cancellationToken);

                await sourceStream.CopyToAsync(destStream, cancellationToken);

                // Copy metadata if requested
                if (options.PreserveMetadata)
                {
                    var metadata = await sourceFile.GetMetadataAsync(cancellationToken);
                    if (metadata.Count > 0)
                    {
                        var metadataDict = new Dictionary<string, string>(metadata);
                        await destFile.SetMetadataAsync(metadataDict, cancellationToken: cancellationToken);
                    }
                }

                copiedFiles++;
                copiedBytes += sourceFile.Size;
            }
        }

        progress?.Report(new CopyProgress
        {
            TotalFiles = totalFiles,
            CopiedFiles = copiedFiles,
            TotalBytes = totalBytes,
            CopiedBytes = copiedBytes
        });
    }

    /// <inheritdoc />
    public async ValueTask<IVfsNode?> GetEntryAsync(VfsPath path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (await FileExistsAsync(path, cancellationToken))
        {
            return await GetFileAsync(path, cancellationToken);
        }

        if (await DirectoryExistsAsync(path, cancellationToken))
        {
            return await GetDirectoryAsync(path, cancellationToken);
        }

        return null;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IVfsNode> ListAsync(
        VfsPath path,
        ListOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        options ??= new ListOptions();

        var directory = await GetDirectoryAsync(path, cancellationToken);
        var pageSize = options.PageSize > 0 ? options.PageSize : _options.DefaultPageSize;

        await foreach (var entry in directory.GetEntriesAsync(
            options.Pattern,
            options.Recursive,
            pageSize,
            cancellationToken))
        {
            if (entry.Type == VfsEntryType.File && !options.IncludeFiles)
                continue;

            if (entry.Type == VfsEntryType.Directory && !options.IncludeDirectories)
                continue;

            yield return entry;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing VirtualFileSystem");
            _disposed = true;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(VirtualFileSystem));
    }
}