using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Exceptions;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Implementations;

/// <summary>
/// Implementation of a virtual directory
/// </summary>
public class VirtualDirectory : IVirtualDirectory
{
    private readonly IVirtualFileSystem _vfs;
    private readonly IMetadataManager _metadataManager;
    private readonly IMemoryCache _cache;
    private readonly ILogger _logger;
    private readonly VfsPath _path;

    private VfsMetadata? _vfsMetadata;
    private bool _metadataLoaded;

    /// <summary>
    /// Initializes a new instance of VirtualDirectory
    /// </summary>
    public VirtualDirectory(
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
    public string Name => _path.IsRoot ? "/" : _path.GetFileName();

    /// <inheritdoc />
    public VfsEntryType Type => VfsEntryType.Directory;

    /// <inheritdoc />
    public DateTimeOffset CreatedOn => _vfsMetadata?.Created ?? DateTimeOffset.MinValue;

    /// <inheritdoc />
    public DateTimeOffset LastModified => _vfsMetadata?.Modified ?? DateTimeOffset.MinValue;

    /// <inheritdoc />
    public async ValueTask<bool> ExistsAsync(CancellationToken cancellationToken = default)
    {
        return await _vfs.DirectoryExistsAsync(_path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Refreshing directory metadata: {Path}", _path);

        // For virtual directories, we might not have explicit metadata unless using a directory strategy
        // that creates marker files
        if (_vfs.Options.DirectoryStrategy != DirectoryStrategy.Virtual)
        {
            var markerKey = GetDirectoryMarkerKey();
            _vfsMetadata = await _metadataManager.GetVfsMetadataAsync(markerKey, cancellationToken);
        }

        _metadataLoaded = true;
    }

    /// <inheritdoc />
    public async ValueTask<IVirtualDirectory> GetParentAsync(CancellationToken cancellationToken = default)
    {
        var parentPath = _path.GetParent();
        return await _vfs.GetDirectoryAsync(parentPath, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IVirtualFile> GetFilesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting files: {Path}, recursive: {Recursive}", _path, recursive);

        await foreach (var entry in GetEntriesInternalAsync(pattern, recursive, pageSize, true, false, cancellationToken))
        {
            if (entry is IVirtualFile file)
            {
                yield return file;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IVirtualDirectory> GetDirectoriesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting directories: {Path}, recursive: {Recursive}", _path, recursive);

        await foreach (var entry in GetEntriesInternalAsync(pattern, recursive, pageSize, false, true, cancellationToken))
        {
            if (entry is IVirtualDirectory directory)
            {
                yield return directory;
            }
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IVfsNode> GetEntriesAsync(
        SearchPattern? pattern = null,
        bool recursive = false,
        int pageSize = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting entries: {Path}, recursive: {Recursive}", _path, recursive);

        await foreach (var entry in GetEntriesInternalAsync(pattern, recursive, pageSize, true, true, cancellationToken))
        {
            yield return entry;
        }
    }

    private async IAsyncEnumerable<IVfsNode> GetEntriesInternalAsync(
        SearchPattern? pattern,
        bool recursive,
        int pageSize,
        bool includeFiles,
        bool includeDirectories,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var effectivePageSize = pageSize > 0 ? pageSize : _vfs.Options.DefaultPageSize;
        if (effectivePageSize <= 0)
        {
            effectivePageSize = int.MaxValue;
        }

        var entriesInPage = 0;
        var pagingEnabled = effectivePageSize != int.MaxValue;

        async ValueTask OnEntryYieldedAsync()
        {
            if (!pagingEnabled)
            {
                return;
            }

            entriesInPage++;
            if (entriesInPage >= effectivePageSize)
            {
                entriesInPage = 0;
                await Task.Yield();
            }
        }

        var prefix = _path.ToBlobKey();
        if (!string.IsNullOrEmpty(prefix) && !prefix.EndsWith('/'))
            prefix += "/";

        var directories = new HashSet<string>();

        await foreach (var blob in _vfs.Storage.GetBlobMetadataListAsync(prefix, cancellationToken))
        {
            if (blob is null)
            {
                continue;
            }

            if (string.IsNullOrEmpty(blob.FullName))
            {
                continue;
            }

            var relativePath = blob.FullName.Length > prefix.Length ?
                blob.FullName[prefix.Length..] : blob.FullName;

            if (string.IsNullOrEmpty(relativePath))
                continue;

            if (!recursive)
            {
                // For non-recursive, check if this blob is in a subdirectory
                var slashIndex = relativePath.IndexOf('/');
                if (slashIndex > 0)
                {
                    // This is in a subdirectory
                    var dirName = relativePath[..slashIndex];
                    if (includeDirectories && directories.Add(dirName))
                    {
                        if (pattern == null || pattern.IsMatch(dirName))
                        {
                            var dirPath = _path.Combine(dirName);
                            yield return new VirtualDirectory(_vfs, _metadataManager, _cache, _logger, dirPath);
                            await OnEntryYieldedAsync();
                        }
                    }
                    continue; // Skip the file itself for non-recursive
                }
            }

            // Handle the file
            if (includeFiles)
            {
                var fileName = System.IO.Path.GetFileName(blob.FullName);
                if (pattern == null || pattern.IsMatch(fileName))
                {
                    var filePath = new VfsPath("/" + blob.FullName);
                    var file = new VirtualFile(_vfs, _metadataManager, _cache, _logger, filePath);
                    yield return file;
                    await OnEntryYieldedAsync();
                }
            }

            // In recursive mode, also track intermediate directories
            if (recursive && includeDirectories)
            {
                var pathParts = relativePath.Split('/');
                var currentPath = "";

                for (int i = 0; i < pathParts.Length - 1; i++) // Exclude the file name itself
                {
                    if (i > 0) currentPath += "/";
                    currentPath += pathParts[i];

                    if (directories.Add(currentPath))
                    {
                        if (pattern == null || pattern.IsMatch(pathParts[i]))
                        {
                            var dirPath = _path.Combine(currentPath);
                            yield return new VirtualDirectory(_vfs, _metadataManager, _cache, _logger, dirPath);
                            await OnEntryYieldedAsync();
                        }
                    }
                }
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask<IVirtualFile> CreateFileAsync(
        string name,
        CreateFileOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("File name cannot be null or empty", nameof(name));

        options ??= new CreateFileOptions();

        _logger.LogDebug("Creating file: {Path}/{Name}", _path, name);

        var filePath = _path.Combine(name);
        var file = await _vfs.GetFileAsync(filePath, cancellationToken);

        if (await file.ExistsAsync(cancellationToken) && !options.Overwrite)
        {
            throw new VfsAlreadyExistsException(filePath);
        }

        // Create empty file with metadata
        var writeOptions = new WriteOptions
        {
            ContentType = options.ContentType,
            Metadata = options.Metadata,
            Overwrite = options.Overwrite
        };

        await file.WriteAllBytesAsync(Array.Empty<byte>(), writeOptions, cancellationToken);

        return file;
    }

    /// <inheritdoc />
    public async ValueTask<IVirtualDirectory> CreateDirectoryAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Directory name cannot be null or empty", nameof(name));

        _logger.LogDebug("Creating directory: {Path}/{Name}", _path, name);

        var dirPath = _path.Combine(name);
        var directory = await _vfs.GetDirectoryAsync(dirPath, cancellationToken);

        // Depending on the directory strategy, we might need to create a marker
        switch (_vfs.Options.DirectoryStrategy)
        {
            case DirectoryStrategy.ZeroByteMarker:
                {
                    var markerKey = dirPath.ToBlobKey() + "/";
                    var uploadOptions = new UploadOptions(markerKey)
                    {
                        MimeType = "application/x-directory"
                    };
                    await _vfs.Storage.UploadAsync(Array.Empty<byte>(), uploadOptions, cancellationToken);
                    break;
                }
            case DirectoryStrategy.DotKeepFile:
                {
                    var keepFile = dirPath.Combine(".keep");
                    var file = await _vfs.GetFileAsync(keepFile, cancellationToken);
                    await file.WriteAllBytesAsync(Array.Empty<byte>(), cancellationToken: cancellationToken);
                    break;
                }
            case DirectoryStrategy.Virtual:
            default:
                // No action needed for virtual directories
                break;
        }

        return directory;
    }

    /// <inheritdoc />
    public async Task<DirectoryStats> GetStatsAsync(
        bool recursive = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting directory stats: {Path}, recursive: {Recursive}", _path, recursive);

        var fileCount = 0;
        var directoryCount = 0;
        var totalSize = 0L;
        var filesByExtension = new Dictionary<string, int>();
        IVirtualFile? largestFile = null;
        DateTimeOffset? oldestModified = null;
        DateTimeOffset? newestModified = null;

        await foreach (var entry in GetEntriesAsync(recursive: recursive, cancellationToken: cancellationToken))
        {
            if (entry.Type == VfsEntryType.File && entry is IVirtualFile file)
            {
                fileCount++;
                totalSize += file.Size;

                var extension = System.IO.Path.GetExtension(file.Name).ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                    extension = "(no extension)";

                filesByExtension[extension] = filesByExtension.GetValueOrDefault(extension, 0) + 1;

                if (largestFile == null || file.Size > largestFile.Size)
                {
                    largestFile = file;
                }

                if (oldestModified == null || file.LastModified < oldestModified)
                {
                    oldestModified = file.LastModified;
                }

                if (newestModified == null || file.LastModified > newestModified)
                {
                    newestModified = file.LastModified;
                }
            }
            else if (entry.Type == VfsEntryType.Directory)
            {
                directoryCount++;
            }
        }

        return new DirectoryStats
        {
            FileCount = fileCount,
            DirectoryCount = directoryCount,
            TotalSize = totalSize,
            FilesByExtension = filesByExtension,
            LargestFile = largestFile,
            OldestModified = oldestModified,
            NewestModified = newestModified
        };
    }

    /// <inheritdoc />
    public async Task<DeleteDirectoryResult> DeleteAsync(
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        return await _vfs.DeleteDirectoryAsync(_path, recursive, cancellationToken);
    }

    private string GetDirectoryMarkerKey()
    {
        return _vfs.Options.DirectoryStrategy switch
        {
            DirectoryStrategy.ZeroByteMarker => _path.ToBlobKey() + "/",
            DirectoryStrategy.DotKeepFile => _path.Combine(".keep").ToBlobKey(),
            _ => _path.ToBlobKey()
        };
    }
}
