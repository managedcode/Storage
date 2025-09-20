using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.Core.Extensions;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Aws.Options;
using ManagedCode.Storage.Tests.Common.TestApp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Testcontainers.Azurite;
using Testcontainers.LocalStack;
using Google.Cloud.Storage.V1;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using Testcontainers.FakeGcsServer;
using Xunit;
using ManagedCode.Storage.Server.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Tests.Common;

[CollectionDefinition(nameof(StorageTestApplication))]
public class StorageTestApplication : WebApplicationFactory<HttpHostProgram>, ICollectionFixture<StorageTestApplication>
{
    private readonly AzuriteContainer _azuriteContainer;
    private readonly LocalStackContainer _localStackContainer;
    private readonly FakeGcsServerContainer _gcpContainer;

    public StorageTestApplication()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage(ContainerImages.Azurite)
            .Build();

        _localStackContainer = new LocalStackBuilder()
            .WithImage(ContainerImages.LocalStack)
            .Build();

        _gcpContainer = new FakeGcsServerBuilder()
            .WithImage(ContainerImages.FakeGCSServer)
            .Build();

        Task.WaitAll(
            _azuriteContainer.StartAsync(),
            _localStackContainer.StartAsync(),
            _gcpContainer.StartAsync()
        );
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddStorageFactory();
            services.AddStorageServer();
            services.AddStorageSignalR();

            services.AddFileSystemStorage(new FileSystemStorageOptions
            {
                BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
            });

            services.AddAzureStorage(new AzureStorageOptions
            {
                Container = "managed-code-bucket",
                ConnectionString = _azuriteContainer.GetConnectionString()
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
                    BaseUri = _gcpContainer.GetConnectionString()
                }
            });

            
            var config = new AmazonS3Config();
            config.ServiceURL = _localStackContainer.GetConnectionString();
            
            services.AddAWSStorage(new AWSStorageOptions
            {
                PublicKey = "localkey",
                SecretKey = "localsecret",
                Bucket = "managed-code-bucket",
                OriginalOptions = config
            });

        });

        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Common", "TestApp"));
        builder.UseEnvironment("Development");
        builder.UseContentRoot(projectDir);
    }

    public override async ValueTask DisposeAsync()
    {
        await Task.WhenAll(
            _azuriteContainer.DisposeAsync().AsTask(),
            _localStackContainer.DisposeAsync().AsTask(),
            _gcpContainer.DisposeAsync().AsTask()
        );
    }
}
