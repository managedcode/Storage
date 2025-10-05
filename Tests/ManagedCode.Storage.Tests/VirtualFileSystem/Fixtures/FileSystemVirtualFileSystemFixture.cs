using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Options;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class FileSystemVirtualFileSystemFixture : IVirtualFileSystemFixture, IAsyncLifetime
{
    private readonly string _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "managedcode-vfs-matrix", Guid.NewGuid().ToString("N"));

    public VirtualFileSystemCapabilities Capabilities { get; } = new();

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_rootPath);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }

        return Task.CompletedTask;
    }

    public async Task<VirtualFileSystemTestContext> CreateContextAsync()
    {
        var baseFolder = Path.Combine(_rootPath, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(baseFolder);

        var options = new FileSystemStorageOptions
        {
            BaseFolder = baseFolder,
            CreateContainerIfNotExists = true
        };

        var storage = new FileSystemStorage(options);
        var cleanup = new Func<ValueTask>(async () =>
        {
            await storage.RemoveContainerAsync();
            if (Directory.Exists(baseFolder))
            {
                Directory.Delete(baseFolder, recursive: true);
            }
        });

        return await VirtualFileSystemTestContext.CreateAsync(
            storage,
            containerName: string.Empty,
            ownsStorage: true,
            serviceProvider: null,
            cleanup: cleanup);
    }
}
