using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Shouldly;
using Google.Cloud.Storage.V1;
using ManagedCode.MimeTypes;
using ManagedCode.Storage.Aws;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Azure;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.Core.Models;
using ManagedCode.Storage.Core.Providers;
using ManagedCode.Storage.FileSystem;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Google;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.Server;
using ManagedCode.Storage.Server.Extensions.Storage;
using ManagedCode.Storage.Tests.Common;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ManagedCode.Storage.Tests.ExtensionsTests;

public class StorageFactoryTests
{
    public StorageFactoryTests()
    {
        ServiceProvider = ConfigureServices();
    }
    
    public ServiceProvider ServiceProvider { get; }

    public static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddFileSystemStorage(new FileSystemStorageOptions
        {
            BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
        });
        
        services.AddAzureStorage(new AzureStorageOptions
        {
            Container = "managed-code-bucket",
            ConnectionString = "UseDevelopmentStorage=true"
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
                BaseUri = "http://localhost:4443"
            }
        });

            
        var config = new AmazonS3Config();
        config.ServiceURL = "http://localhost:4443";
            
        services.AddAWSStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = config
        });


        // Add factory
        services.AddStorageFactory();

        return services.BuildServiceProvider();
    }

    [Fact]
    public void CreateAzureStorage()
    {
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateStorage(new AzureStorageOptions
        {
            Container = "managed-code-bucket",
            ConnectionString = "UseDevelopmentStorage=true"
        });
        storage.GetType().ShouldBe(typeof(AzureStorage));
    }
    
    [Fact]
    public void CreateAwsStorage()
    {
        var config = new AmazonS3Config();
        config.ServiceURL = "http://localhost:4443";
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateStorage(new AWSStorageOptions
        {
            PublicKey = "localkey",
            SecretKey = "localsecret",
            Bucket = "managed-code-bucket",
            OriginalOptions = config
        });
        storage.GetType().ShouldBe(typeof(AWSStorage));
    }

    [Fact]
    public void CreateGcpStorage()
    {
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateStorage(new GCPStorageOptions
        {
            BucketOptions = new BucketOptions
            {
                ProjectId = "api-project-0000000000000",
                Bucket = "managed-code-bucket"
            },
            StorageClientBuilder = new StorageClientBuilder
            {
                UnauthenticatedAccess = true,
                BaseUri = "http://localhost:4443"
            }
        });
        storage.GetType().ShouldBe(typeof(GCPStorage));
    }
    
    [Fact]
    public void UpdateAzureStorage()
    {
        var containerName = Guid.NewGuid().ToString();
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateAzureStorage(containerName);
        storage.StorageClient
            .ShouldNotBeNull();
        storage.StorageClient.Name
            .ShouldBe(containerName);

    }
    
    [Fact]
    public void UpdateAwsStorage()
    {
        var containerName = Guid.NewGuid().ToString();
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateAWSStorage(containerName);
        storage.StorageClient
            .ShouldNotBeNull();
    }
    
    [Fact]
    public void UpdateGcpStorage()
    {
        var containerName = Guid.NewGuid().ToString();
        var factory = ServiceProvider.GetRequiredService<IStorageFactory>();
        var storage = factory.CreateGCPStorage(containerName);
        storage.StorageClient
            .ShouldNotBeNull();
    }
 
}

