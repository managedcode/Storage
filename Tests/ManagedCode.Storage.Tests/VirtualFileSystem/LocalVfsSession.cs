using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

internal sealed class LocalVfsSession : IAsyncDisposable
{
    private readonly string _root;
    private readonly ServiceProvider _provider;

    private LocalVfsSession(string root, ServiceProvider provider, FileSystemStorage storage)
    {
        _root = root;
        _provider = provider;
        Storage = storage;
        FileSystem = provider.GetRequiredService<IVirtualFileSystem>();
    }

    public FileSystemStorage Storage { get; }
    public string RootDirectory => _root;
    public IVirtualFileSystem FileSystem { get; }

    public static async Task<LocalVfsSession> CreateAsync(bool cache = true,
        DirectoryStrategy strategy = DirectoryStrategy.Virtual, int pageSize = 100)
    {
        var root = Path.Combine(Path.GetTempPath(), "managedcode-vfs-contracts", Guid.NewGuid().ToString("N"));
        var storage = new FileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = root,
            CreateContainerIfNotExists = true
        });
        (await storage.CreateContainerAsync(TestContext.Current.CancellationToken)).IsSuccess.ShouldBeTrue();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddVirtualFileSystem(storage, options =>
        {
            options.DefaultContainer = string.Empty;
            options.EnableCache = cache;
            options.DirectoryStrategy = strategy;
            options.DefaultPageSize = pageSize;
        });
        return new LocalVfsSession(root, services.BuildServiceProvider(), storage);
    }

    public async ValueTask DisposeAsync()
    {
        await _provider.DisposeAsync();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }
}
