using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Implementations;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;

namespace ManagedCode.Storage.VirtualFileSystem.Extensions;

/// <summary>
/// Extension methods for registering Virtual File System services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Virtual File System services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configureOptions">Optional configuration action for VFS options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddVirtualFileSystem(
        this IServiceCollection services,
        Action<VfsOptions>? configureOptions = null)
    {
        // Configure options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.Configure<VfsOptions>(_ => { });
        }

        // Register core services
        services.TryAddSingleton<IMemoryCache, MemoryCache>();

        // Register VFS services
        services.TryAddScoped<IVirtualFileSystem, Implementations.VirtualFileSystem>();
        services.TryAddSingleton<IVirtualFileSystemManager, VirtualFileSystemManager>();

        // Register metadata manager (this will be overridden by storage-specific registrations)
        services.TryAddScoped<IMetadataManager, DefaultMetadataManager>();

        return services;
    }

    /// <summary>
    /// Adds Virtual File System with a specific storage provider
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="storage">The storage provider</param>
    /// <param name="configureOptions">Optional configuration action for VFS options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddVirtualFileSystem(
        this IServiceCollection services,
        IStorage storage,
        Action<VfsOptions>? configureOptions = null)
    {
        services.AddSingleton(storage);
        return services.AddVirtualFileSystem(configureOptions);
    }

    /// <summary>
    /// Adds Virtual File System with a factory for creating storage providers
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="storageFactory">Factory function for creating storage providers</param>
    /// <param name="configureOptions">Optional configuration action for VFS options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddVirtualFileSystem(
        this IServiceCollection services,
        Func<IServiceProvider, IStorage> storageFactory,
        Action<VfsOptions>? configureOptions = null)
    {
        services.AddScoped(storageFactory);
        return services.AddVirtualFileSystem(configureOptions);
    }
}

/// <summary>
/// Default metadata manager implementation
/// </summary>
internal class DefaultMetadataManager : BaseMetadataManager
{
    private readonly IStorage _storage;
    private readonly ILogger<DefaultMetadataManager> _logger;

    protected override string MetadataPrefix => "x-vfs-";

    public DefaultMetadataManager(IStorage storage, ILogger<DefaultMetadataManager> logger)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task SetVfsMetadataAsync(
        string blobName,
        VfsMetadata metadata,
        IDictionary<string, string>? customMetadata = null,
        string? expectedETag = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting VFS metadata for: {BlobName}", blobName);

        var metadataDict = BuildMetadataDictionary(metadata, customMetadata);

        // Use the storage provider's metadata setting capability
        // Note: This is a simplified implementation. Real implementation would depend on the storage provider
        try
        {
            var blobMetadata = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
            if (blobMetadata.IsSuccess && blobMetadata.Value != null)
            {
                // Update existing metadata
                var existingMetadata = blobMetadata.Value.Metadata ?? new Dictionary<string, string>();
                foreach (var kvp in metadataDict)
                {
                    existingMetadata[kvp.Key] = kvp.Value;
                }

                // Note: Most storage providers don't have a direct "set metadata" operation
                // This would typically require re-uploading the blob with new metadata
                _logger.LogWarning("Metadata update not fully implemented for this storage provider");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting VFS metadata for: {BlobName}", blobName);
            throw;
        }
    }

    public override async Task<VfsMetadata?> GetVfsMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting VFS metadata for: {BlobName}", blobName);

        try
        {
            var blobMetadata = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
            if (!blobMetadata.IsSuccess || blobMetadata.Value?.Metadata == null)
            {
                return null;
            }

            return ParseVfsMetadata(blobMetadata.Value.Metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting VFS metadata for: {BlobName}", blobName);
            return null;
        }
    }

    public override async Task<IReadOnlyDictionary<string, string>> GetCustomMetadataAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting custom metadata for: {BlobName}", blobName);

        try
        {
            var blobMetadata = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
            if (!blobMetadata.IsSuccess || blobMetadata.Value?.Metadata == null)
            {
                return new Dictionary<string, string>();
            }

            return ExtractCustomMetadata(blobMetadata.Value.Metadata);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting custom metadata for: {BlobName}", blobName);
            return new Dictionary<string, string>();
        }
    }

    public override async Task<BlobMetadata?> GetBlobInfoAsync(
        string blobName,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting blob info for: {BlobName}", blobName);

        try
        {
            var result = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
            return result.IsSuccess ? result.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Blob not found or error getting blob info: {BlobName}", blobName);
            return null;
        }
    }
}

/// <summary>
/// Implementation of Virtual File System Manager
/// </summary>
internal class VirtualFileSystemManager : IVirtualFileSystemManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<VirtualFileSystemManager> _logger;
    private readonly Dictionary<string, IVirtualFileSystem> _mounts = new();
    private bool _disposed;

    public VirtualFileSystemManager(
        IServiceProvider serviceProvider,
        ILogger<VirtualFileSystemManager> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task MountAsync(
        string mountPoint,
        IStorage storage,
        VfsOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mountPoint))
            throw new ArgumentException("Mount point cannot be null or empty", nameof(mountPoint));

        if (storage == null)
            throw new ArgumentNullException(nameof(storage));

        _logger.LogDebug("Mounting storage at: {MountPoint}", mountPoint);

        // Normalize mount point
        mountPoint = mountPoint.TrimEnd('/');
        if (!mountPoint.StartsWith('/'))
            mountPoint = '/' + mountPoint;

        // Create VFS instance
        var cache = _serviceProvider.GetRequiredService<IMemoryCache>();
        var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
        var metadataManager = new DefaultMetadataManager(storage, loggerFactory.CreateLogger<DefaultMetadataManager>());

        var vfsOptions = Microsoft.Extensions.Options.Options.Create(options ?? new VfsOptions());
        var vfsLogger = loggerFactory.CreateLogger<Implementations.VirtualFileSystem>();

        var vfs = new Implementations.VirtualFileSystem(storage, metadataManager, vfsOptions, cache, vfsLogger);

        _mounts[mountPoint] = vfs;

        _logger.LogInformation("Storage mounted successfully at: {MountPoint}", mountPoint);
    }

    public async Task UnmountAsync(string mountPoint, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(mountPoint))
            throw new ArgumentException("Mount point cannot be null or empty", nameof(mountPoint));

        // Normalize mount point
        mountPoint = mountPoint.TrimEnd('/');
        if (!mountPoint.StartsWith('/'))
            mountPoint = '/' + mountPoint;

        _logger.LogDebug("Unmounting storage from: {MountPoint}", mountPoint);

        if (_mounts.TryGetValue(mountPoint, out var vfs))
        {
            await vfs.DisposeAsync();
            _mounts.Remove(mountPoint);
            _logger.LogInformation("Storage unmounted from: {MountPoint}", mountPoint);
        }
        else
        {
            _logger.LogWarning("No mount found at: {MountPoint}", mountPoint);
        }
    }

    public IVirtualFileSystem GetMount(string mountPoint)
    {
        if (string.IsNullOrWhiteSpace(mountPoint))
            throw new ArgumentException("Mount point cannot be null or empty", nameof(mountPoint));

        // Normalize mount point
        mountPoint = mountPoint.TrimEnd('/');
        if (!mountPoint.StartsWith('/'))
            mountPoint = '/' + mountPoint;

        if (_mounts.TryGetValue(mountPoint, out var vfs))
        {
            return vfs;
        }

        throw new ArgumentException($"No mount found at: {mountPoint}", nameof(mountPoint));
    }

    public IReadOnlyDictionary<string, IVirtualFileSystem> GetMounts()
    {
        return new ReadOnlyDictionary<string, IVirtualFileSystem>(_mounts);
    }

    public (string MountPoint, VfsPath RelativePath) ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        // Normalize path
        if (!path.StartsWith('/'))
            path = '/' + path;

        // Find the longest matching mount point
        var bestMatch = "";
        foreach (var mountPoint in _mounts.Keys.OrderByDescending(mp => mp.Length))
        {
            if (path.StartsWith(mountPoint + "/") || path == mountPoint)
            {
                bestMatch = mountPoint;
                break;
            }
        }

        if (string.IsNullOrEmpty(bestMatch))
        {
            throw new ArgumentException($"No mount point found for path: {path}", nameof(path));
        }

        var relativePath = path == bestMatch ? "/" : path[bestMatch.Length..];
        return (bestMatch, new VfsPath(relativePath));
    }

    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _logger.LogDebug("Disposing VirtualFileSystemManager");

            foreach (var vfs in _mounts.Values)
            {
                await vfs.DisposeAsync();
            }

            _mounts.Clear();
            _disposed = true;
        }
    }
}