using System;
using Shouldly;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Exceptions;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.Storages.GCS;

public class GCSConfigTests
{
    [Fact]
    public void BadConfigurationForStorage_WithoutProjectId_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorage(opt =>
        {
            opt.BucketOptions = new BucketOptions
            {
                Bucket = "managed-code-bucket"
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/"
            };
        });

        Should.Throw<BadConfigurationException>(action);
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutBucket_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorage(opt =>
        {
            opt.BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000"
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443/storage/v1/"
            };
        });

        Should.Throw<BadConfigurationException>(action);
    }

    [Fact]
    public void BadConfigurationForStorage_WithoutStorageClientBuilderAndGoogleCredential_ThrowException()
    {
        var services = new ServiceCollection();

        Action action = () => services.AddGCPStorageAsDefault(opt =>
        {
            opt.BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            };
        });

        Should.Throw<BadConfigurationException>(action);
    }

    [Fact]
    public void StorageAsDefaultTest()
    {
        var storage = GCSConfigurator.ConfigureServices("test")
            .GetService<IGCPStorage>();
        var defaultStorage = GCSConfigurator.ConfigureServices("test")
            .GetService<IStorage>();
        storage?.GetType()
            .FullName
            .ShouldBe(defaultStorage?.GetType()
                .FullName);
    }
}
