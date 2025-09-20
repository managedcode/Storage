using System;
using System.IO;
using System.Threading.Tasks;
using Shouldly;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.VirtualFileSystem.Core;
using ManagedCode.Storage.VirtualFileSystem.Extensions;
using ManagedCode.Storage.VirtualFileSystem.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem;

public class VirtualFileSystemManagerTests : IAsyncLifetime
{
    private readonly string _basePath = Path.Combine(Path.GetTempPath(), "managedcode-vfs-manager", Guid.NewGuid().ToString());
    private ServiceProvider _serviceProvider = null!;
    private IStorage _storage = null!;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(_basePath);
        var services = new ServiceCollection();
        services.AddLogging();

        services.AddSingleton<IStorage>(_ => new FileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = _basePath,
            CreateContainerIfNotExists = true
        }));

        services.AddVirtualFileSystem(options =>
        {
            options.DefaultContainer = string.Empty;
            options.EnableCache = true;
        });

        _serviceProvider = services.BuildServiceProvider();
        _storage = _serviceProvider.GetRequiredService<IStorage>();
        await _storage.CreateContainerAsync();
    }

    public async Task DisposeAsync()
    {
        if (_serviceProvider.GetService<IVirtualFileSystemManager>() is IAsyncDisposable asyncManager)
        {
            await asyncManager.DisposeAsync();
        }

        (_storage as IDisposable)?.Dispose();
        await _serviceProvider.DisposeAsync();

        if (Directory.Exists(_basePath))
        {
            Directory.Delete(_basePath, recursive: true);
        }
    }

    [Fact]
    public async Task MountAndResolvePaths_ShouldWork()
    {
        var manager = _serviceProvider.GetRequiredService<IVirtualFileSystemManager>();
        await manager.MountAsync("/fs", _storage, new VfsOptions { DefaultContainer = string.Empty });

        var vfs = manager.GetMount("/fs");
        var file = await vfs.GetFileAsync(new VfsPath("/sample.txt"));
        await file.WriteAllTextAsync("manager-test");

        var (mountPoint, relativePath) = manager.ResolvePath("/fs/sample.txt");
        mountPoint.ShouldBe("/fs");
        relativePath.Value.ShouldBe("/sample.txt");

        var mounts = manager.GetMounts();
        mounts.ShouldContainKey("/fs");

        await manager.UnmountAsync("/fs");
        mounts = manager.GetMounts();
        mounts.ShouldBeEmpty();

        Func<IVirtualFileSystem> action = () => manager.GetMount("/fs");
        Should.Throw<ArgumentException>(action);
    }
}
