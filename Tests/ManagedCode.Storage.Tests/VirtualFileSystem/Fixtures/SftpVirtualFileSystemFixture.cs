using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Sftp;
using ManagedCode.Storage.Tests.Storages.Sftp;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Sftp;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class SftpVirtualFileSystemFixture : IVirtualFileSystemFixture, IAsyncLifetime
{
    private SftpContainer _container = null!;

    public VirtualFileSystemCapabilities Capabilities { get; } = new(
        Enabled: false,
        SupportsListing: false,
        SupportsDirectoryDelete: false,
        SupportsDirectoryCopy: false,
        SupportsMove: false,
        SupportsDirectoryStats: false);

    public async Task InitializeAsync()
    {
        _container = SftpContainerFactory.Create();
        await _container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }

    public async Task<VirtualFileSystemTestContext> CreateContextAsync()
    {
        var host = _container.GetHost();
        var port = _container.GetPort();
        var username = SftpContainerFactory.Username;
        var password = SftpContainerFactory.Password;
        var remoteDirectory = $"{SftpContainerFactory.RemoteDirectory}/vfs-{Guid.NewGuid():N}";

        var provider = SftpConfigurator.ConfigureServices(host, port, username, password, remoteDirectory);
        var storage = provider.GetRequiredService<IStorage>();

        async ValueTask Cleanup()
        {
            await storage.RemoveContainerAsync();
        }

        return await VirtualFileSystemTestContext.CreateAsync(
            storage,
            remoteDirectory,
            ownsStorage: false,
            serviceProvider: provider,
            cleanup: Cleanup);
    }
}
