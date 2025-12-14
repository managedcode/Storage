using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.CloudKit.Extensions;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudKit;

public class CloudKitDependencyInjectionTests
{
    [Fact]
    public void CloudKit_AddAsDefault_ShouldResolveIStorage()
    {
        var services = new ServiceCollection();
        services.AddCloudKitStorageAsDefault(options =>
        {
            options.ContainerId = "iCloud.com.example.app";
            options.ApiToken = "test-token";
            options.Environment = CloudKitEnvironment.Development;
            options.Database = CloudKitDatabase.Public;
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredService<IStorage>();
        var typed = provider.GetRequiredService<ICloudKitStorage>();

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void CloudKit_AddAsDefault_Keyed_ShouldResolveKeyedIStorage()
    {
        var services = new ServiceCollection();
        services.AddCloudKitStorageAsDefault("tenant-a", options =>
        {
            options.ContainerId = "iCloud.com.example.app";
            options.ApiToken = "test-token";
        });

        using var provider = services.BuildServiceProvider();
        var storage = provider.GetRequiredKeyedService<IStorage>("tenant-a");
        var typed = provider.GetRequiredKeyedService<ICloudKitStorage>("tenant-a");

        storage.ShouldBeSameAs(typed);
    }

    [Fact]
    public void CloudKit_WhenContainerIdMissing_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddCloudKitStorage(options => options.ApiToken = "test-token"));
    }

    [Fact]
    public void CloudKit_WhenApiTokenAndServerKeyProvided_ShouldThrow()
    {
        var services = new ServiceCollection();
        Should.Throw<BadConfigurationException>(() =>
            services.AddCloudKitStorage(options =>
            {
                options.ContainerId = "iCloud.com.example.app";
                options.ApiToken = "test-token";
                options.ServerToServerKeyId = "kid";
                options.ServerToServerPrivateKeyPem = "-----BEGIN PRIVATE KEY-----\n...\n-----END PRIVATE KEY-----";
            }));
    }
}
