using System;
using System.Threading.Tasks;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.GCS;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.FakeGcsServer;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class GcsVirtualFileSystemFixture : IVirtualFileSystemFixture, IAsyncLifetime
{
    private FakeGcsServerContainer _container = null!;

    public VirtualFileSystemCapabilities Capabilities { get; } = new(Enabled: false);

    public async Task InitializeAsync()
    {
        _container = new FakeGcsServerBuilder()
            .WithImage(ContainerImages.FakeGCSServer)
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
        var bucketName = $"vfs-{Guid.NewGuid():N}";
        var baseUri = _container.GetConnectionString();

        var services = new ServiceCollection();
        services.AddLogging();

        static BucketOptions CreateBucketOptions(string projectId, string bucket) => new()
        {
            ProjectId = projectId,
            Bucket = bucket
        };

        var projectId = "api-project-0000000000000";

        services.AddGCPStorageAsDefault(options =>
        {
            options.BucketOptions = CreateBucketOptions(projectId, bucketName);
            options.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = baseUri
            };
        });

        services.AddGCPStorage(new GCPStorageOptions
        {
            BucketOptions = CreateBucketOptions(projectId, bucketName),
            StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = baseUri
            }
        });

        var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();

        async ValueTask Cleanup()
        {
            await storage.RemoveContainerAsync();
        }

        return await VirtualFileSystemTestContext.CreateAsync(
            storage,
            bucketName,
            ownsStorage: false,
            serviceProvider: provider,
            cleanup: Cleanup);
    }
}
