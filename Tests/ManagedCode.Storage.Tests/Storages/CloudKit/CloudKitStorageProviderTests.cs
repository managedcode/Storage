using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ManagedCode.Storage.CloudKit;
using ManagedCode.Storage.CloudKit.Clients;
using ManagedCode.Storage.CloudKit.Options;
using ManagedCode.Storage.Core;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.CloudKit;

public class CloudKitStorageProviderTests
{
    [Fact]
    public void CloudKitStorageProvider_CreateStorage_ShouldUseCloudKitOptions()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var defaultOptions = new CloudKitStorageOptions
        {
            ContainerId = "iCloud.com.example.app",
            ApiToken = "test-token",
            Client = new StubCloudKitClient()
        };

        var provider = new CloudKitStorageProvider(serviceProvider, defaultOptions);
        provider.StorageOptionsType.ShouldBe(typeof(CloudKitStorageOptions));

        var cloned = provider.GetDefaultOptions().ShouldBeOfType<CloudKitStorageOptions>();
        cloned.ShouldNotBeSameAs(defaultOptions);
        cloned.ContainerId.ShouldBe(defaultOptions.ContainerId);
        cloned.ApiToken.ShouldBe(defaultOptions.ApiToken);
        cloned.Client.ShouldBeSameAs(defaultOptions.Client);

        var storage = provider.CreateStorage<ICloudKitStorage, CloudKitStorageOptions>(cloned);
        storage.ShouldBeOfType<CloudKitStorage>();
    }

    [Fact]
    public void CloudKitStorageProvider_WhenOptionsWrong_ShouldThrow()
    {
        using var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var provider = new CloudKitStorageProvider(serviceProvider, new CloudKitStorageOptions { ContainerId = "iCloud.com.example.app", ApiToken = "test-token", Client = new StubCloudKitClient() });

        Should.Throw<ArgumentException>(() =>
            provider.CreateStorage<ICloudKitStorage, FakeOptions>(new FakeOptions()));
    }

    private sealed class FakeOptions : IStorageOptions
    {
        public bool CreateContainerIfNotExists { get; set; }
    }

    private sealed class StubCloudKitClient : ICloudKitClient
    {
        public Task<CloudKitRecord> UploadAsync(string recordName, string internalPath, Stream content, string contentType, CancellationToken cancellationToken)
            => throw new NotImplementedException();

        public Task<Stream> DownloadAsync(string recordName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> DeleteAsync(string recordName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<bool> ExistsAsync(string recordName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public Task<CloudKitRecord?> GetRecordAsync(string recordName, CancellationToken cancellationToken) => throw new NotImplementedException();

        public IAsyncEnumerable<CloudKitRecord> QueryByPathPrefixAsync(string pathPrefix, CancellationToken cancellationToken) => throw new NotImplementedException();
    }
}

