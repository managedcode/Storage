using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Metadata;
using ManagedCode.Storage.VirtualFileSystem.Options;
using VfsImplementation = ManagedCode.Storage.VirtualFileSystem.Implementations.VirtualFileSystem;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class VirtualFileSystemTestContext : IAsyncDisposable
{
    private readonly bool _ownsStorage;
    private readonly IServiceProvider? _serviceProvider;
    private readonly Func<ValueTask>? _cleanup;
    private readonly MemoryCache _cache;

    private VirtualFileSystemTestContext(
        IStorage storage,
        TestMetadataManager metadataManager,
        VfsImplementation fileSystem,
        MemoryCache cache,
        bool ownsStorage,
        IServiceProvider? serviceProvider,
        string containerName,
        Func<ValueTask>? cleanup)
    {
        Storage = storage;
        MetadataManager = metadataManager;
        FileSystem = fileSystem;
        ContainerName = containerName;
        _cache = cache;
        _ownsStorage = ownsStorage;
        _serviceProvider = serviceProvider;
        _cleanup = cleanup;
    }

    public IStorage Storage { get; }
    public TestMetadataManager MetadataManager { get; }
    public VfsImplementation FileSystem { get; }
    public string ContainerName { get; }

    public static async Task<VirtualFileSystemTestContext> CreateAsync(
        IStorage storage,
        string containerName,
        bool ownsStorage,
        IServiceProvider? serviceProvider,
        Func<ValueTask>? cleanup = null)
    {
        var metadataManager = new TestMetadataManager(storage);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new VfsOptions
        {
            DefaultContainer = containerName,
            DirectoryStrategy = DirectoryStrategy.Virtual,
            EnableCache = true
        });

        var vfs = new VfsImplementation(
            storage,
            metadataManager,
            options,
            cache,
            NullLogger<VfsImplementation>.Instance);

        var createResult = await storage.CreateContainerAsync();
        if (!createResult.IsSuccess)
        {
            throw new InvalidOperationException($"Failed to create container '{containerName}'.");
        }

        return new VirtualFileSystemTestContext(storage, metadataManager, vfs, cache, ownsStorage, serviceProvider, containerName, cleanup);
    }

    public async ValueTask DisposeAsync()
    {
        await FileSystem.DisposeAsync();

        if (_cleanup is not null)
        {
            await _cleanup();
        }

        _cache.Dispose();

        if (_ownsStorage)
        {
            switch (Storage)
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync();
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        if (_serviceProvider is IAsyncDisposable asyncProvider)
        {
            await asyncProvider.DisposeAsync();
        }
        else if (_serviceProvider is IDisposable disposableProvider)
        {
            disposableProvider.Dispose();
        }
    }
}

public sealed class TestMetadataManager : IMetadataManager
{
    private readonly IStorage _storage;
    private readonly ConcurrentDictionary<string, VfsMetadata> _metadata = new();
    private readonly ConcurrentDictionary<string, IReadOnlyDictionary<string, string>> _customMetadata = new();

    public TestMetadataManager(IStorage storage)
    {
        _storage = storage;
    }

    public int BlobInfoRequests { get; private set; }
    public int CustomMetadataRequests { get; private set; }

    public void ResetCounters()
    {
        BlobInfoRequests = 0;
        CustomMetadataRequests = 0;
    }

    public Task SetVfsMetadataAsync(string blobName, VfsMetadata metadata, IDictionary<string, string>? customMetadata = null, string? expectedETag = null, CancellationToken cancellationToken = default)
    {
        _metadata[blobName] = metadata;
        _customMetadata[blobName] = customMetadata is null
            ? new Dictionary<string, string>()
            : new Dictionary<string, string>(customMetadata);
        return Task.CompletedTask;
    }

    public Task<VfsMetadata?> GetVfsMetadataAsync(string blobName, CancellationToken cancellationToken = default)
    {
        _metadata.TryGetValue(blobName, out var metadata);
        return Task.FromResult(metadata);
    }

    public Task<IReadOnlyDictionary<string, string>> GetCustomMetadataAsync(string blobName, CancellationToken cancellationToken = default)
    {
        CustomMetadataRequests++;
        if (_customMetadata.TryGetValue(blobName, out var metadata))
        {
            return Task.FromResult(metadata);
        }

        return Task.FromResult<IReadOnlyDictionary<string, string>>(new Dictionary<string, string>());
    }

    public async Task<BlobMetadata?> GetBlobInfoAsync(string blobName, CancellationToken cancellationToken = default)
    {
        BlobInfoRequests++;
        var result = await _storage.GetBlobMetadataAsync(blobName, cancellationToken);
        return result.IsSuccess ? result.Value : null;
    }
}
