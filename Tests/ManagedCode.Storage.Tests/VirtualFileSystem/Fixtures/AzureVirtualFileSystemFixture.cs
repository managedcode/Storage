using System;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class AzureVirtualFileSystemFixture : IVirtualFileSystemFixture, IAsyncLifetime
{
    private AzuriteContainer _container = null!;

    public VirtualFileSystemCapabilities Capabilities { get; } = new();

    public async Task InitializeAsync()
    {
        _container = new AzuriteBuilder()
            .WithImage(ContainerImages.Azurite)
            .WithCommand("--skipApiVersionCheck")
            .Build();

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
        var containerName = $"vfs-{Guid.NewGuid():N}";
        var connectionString = _container.GetConnectionString();

        var services = new ServiceCollection();

        services.AddLogging();

        services.AddAzureStorageAsDefault(options =>
        {
            options.ConnectionString = connectionString;
            options.Container = containerName;
            options.CreateContainerIfNotExists = true;
        });

        services.AddAzureStorage(new AzureStorageOptions
        {
            ConnectionString = connectionString,
            Container = containerName,
            CreateContainerIfNotExists = true
        });

        var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();

        async ValueTask Cleanup()
        {
            await storage.RemoveContainerAsync();
        }

        return await VirtualFileSystemTestContext.CreateAsync(
            storage,
            containerName,
            ownsStorage: false,
            serviceProvider: provider,
            cleanup: Cleanup);
    }
}
