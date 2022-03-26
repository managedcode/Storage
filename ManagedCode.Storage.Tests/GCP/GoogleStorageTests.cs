using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Gcp;
using ManagedCode.Storage.Gcp.Extensions;
using ManagedCode.Storage.Gcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.GCP;

public class GoogleStorageTests : StorageBaseTests
{
    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddGCPStorageAsDefault(opt =>
        {
            opt.BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/",
            };
        });

        services.AddGCPStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            },
            StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/",
            }
        });
        return services.BuildServiceProvider();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutProjectId_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorage(opt =>
        {
            opt.BucketOptions = new BucketOptions()
            {
                Bucket = "managed-code-bucket",
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/",
            };
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutBucket_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorage(opt =>
        {
            opt.BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/",
            };
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutStorageClientBuilderAndGoogleCredential_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorageAsDefault(opt =>
        {
            opt.BucketOptions = new BucketOptions()
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket",
            };
        });

        action.Should().Throw<BadConfigurationException>();
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = ServiceProvider.GetService<IGCPStorage>();
        var defaultStorage = ServiceProvider.GetService<IStorage>();
        storage?.GetType().FullName.Should().Be(defaultStorage?.GetType().FullName);
    }

    [Fact]
    public override async Task GetBlobsAsync()
    {
        var fileList = await CreateFileList(nameof(GetBlobsAsync), 5);

        var blobList = fileList.Select(f => f.FileName).ToList();

        var result = await Storage.GetBlobsAsync(blobList).ToListAsync();

        foreach (var blobMetadata in result)
        {
            blobMetadata.Name.Should().NotBeNull();

            // Uri null for GCP storage emulator
            // result.Uri.Should().NotBeNull();
        }

        foreach (var item in fileList)
        {
            await DeleteFileAsync(item.FileName);
        }
    }

    [Fact]
    public override async Task GetBlobAsync()
    {
        const string uploadContent = $"test {nameof(GetBlobAsync)}";
        const string fileName = $"{nameof(GetBlobAsync)}.txt";

        await PrepareFileToTest(fileName, uploadContent);

        var result = await Storage.GetBlobAsync(fileName);

        result.Should().NotBeNull();
        result!.Name.Should().Be(fileName);

        // Uri null for GCP storage emulator
        // result.Uri.Should().NotBeNull();

        await DeleteFileAsync(fileName);
    }
}