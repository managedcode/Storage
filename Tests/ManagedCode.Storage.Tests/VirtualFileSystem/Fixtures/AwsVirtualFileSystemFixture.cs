using System;
using System.Threading.Tasks;
using Amazon.S3;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Tests.Common;
using ManagedCode.Storage.Tests.Storages.AWS;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests.VirtualFileSystem.Fixtures;

public sealed class AwsVirtualFileSystemFixture : IVirtualFileSystemFixture, IAsyncLifetime
{
    private LocalStackContainer _container = null!;

    public VirtualFileSystemCapabilities Capabilities { get; } = new(
        Enabled: false,
        SupportsListing: false,
        SupportsDirectoryDelete: false,
        SupportsDirectoryCopy: false,
        SupportsMove: false,
        SupportsDirectoryStats: false);

    public async Task InitializeAsync()
    {
        _container = AwsContainerFactory.Create();
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
        var serviceUrl = _container.GetConnectionString();

        var awsConfig = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddAWSStorageAsDefault(options =>
        {
            options.PublicKey = "localkey";
            options.SecretKey = "localsecret";
            options.Bucket = bucketName;
            options.OriginalOptions = awsConfig;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = bucketName,
            OriginalOptions = awsConfig
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
