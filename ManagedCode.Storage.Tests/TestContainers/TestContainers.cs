using System;
using System.Threading.Tasks;
using Amazon.S3;
using Azure;
using Azure.Storage.Blobs;
using DotNet.Testcontainers.Containers;
using FluentAssertions;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.Azurite;
using Testcontainers.GCS;
using Testcontainers.LocalStack;
using Xunit;

namespace ManagedCode.Storage.Tests;

public abstract class BaseContainer<T> : IAsyncLifetime where T : DockerContainer
{
    protected T Container { get; private set; }
    protected abstract T Build();
    protected abstract ServiceProvider ConfigureServices();
    
    protected IStorage Storage { get; private set; }
    protected ServiceProvider ServiceProvider { get; private set; }
    
    
    public async Task InitializeAsync()
    {
        Container = Build();
        await Container.StartAsync();
        ServiceProvider = ConfigureServices();
        Storage = ServiceProvider.GetService<IStorage>()!;
    }

    public Task DisposeAsync()
    {
        return Container.DisposeAsync().AsTask();
    }
}


public abstract class UploadTests<T> : BaseContainer<T>  where T : DockerContainer
{
    #region CreateContainer

    [Fact]
    public async Task CreateContainerAsync()
    {
        await FluentActions.Awaiting(() => Storage.CreateContainerAsync())
            .Should()
            .NotThrowAsync<Exception>();
    }

    #endregion
}



public class AzureStorageTests : UploadTests<AzuriteContainer>
{
    protected override AzuriteContainer Build()
    {
        return new AzuriteBuilder().Build();
    }
    
    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddAzureStorageAsDefault(opt =>
        {
            opt.Container = "managed-code-bucket";
            opt.ConnectionString = Container.GetConnectionString();
        });

        services.AddAzureStorage(new AzureStorageOptions
        {
            Container = "managed-code-bucket",
            ConnectionString = Container.GetConnectionString()
        });
        
        return services.BuildServiceProvider();
    }
}

public class AmazonStorageTests : UploadTests<LocalStackContainer>
{
    protected override LocalStackContainer Build()
    {
        return new LocalStackBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        var config = new AmazonS3Config();
        config.ServiceURL = Container.GetConnectionString();
        
        services.AddAWSStorageAsDefault(opt =>
        {
            opt.PublicKey = "localkey";
            opt.SecretKey = "localsecret";
            opt.Bucket = "managed-code-bucket";
            opt.OriginalOptions = config;
        });

        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = config
        });
        return services.BuildServiceProvider();
    }
    
}

public class GCSStorageTests : UploadTests<GCSContainer>
{
    protected override GCSContainer Build()
    {
        return new GCSBuilder().Build();
    }

    protected override ServiceProvider ConfigureServices()
    {

        var services = new ServiceCollection();

        services.AddGCPStorageAsDefault(opt =>
        {
            opt.BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            };
            opt.StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = Container.GetConnectionString()
            };
        });

        services.AddGCPStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            },
            StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = Container.GetConnectionString() 
            }
        });
        return services.BuildServiceProvider();
    }
    
}

