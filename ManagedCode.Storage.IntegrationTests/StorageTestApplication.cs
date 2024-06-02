using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using ManagedCode.Storage.Aws.Extensions;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Google.Extensions;
using ManagedCode.Storage.Google.Options;
using ManagedCode.Storage.IntegrationTests.TestApp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests;

[CollectionDefinition(nameof(StorageTestApplication))]
public class StorageTestApplication : WebApplicationFactory<HttpHostProgram>, ICollectionFixture<StorageTestApplication>
{
    private readonly AzuriteContainer _azuriteContainer;

    public StorageTestApplication()
    {
        _azuriteContainer = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:3.29.0")
            .Build();
        
        _azuriteContainer.StartAsync().Wait();
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            #region Add FileSystemStorage

            services.AddFileSystemStorage(new FileSystemStorageOptions
            {
                BaseFolder = Path.Combine(Environment.CurrentDirectory, "managed-code-bucket")
            });

            #endregion

            #region Add AzureStorage

            services.AddAzureStorage(new AzureStorageOptions
            {
                Container = "managed-code-bucket",
                ConnectionString = _azuriteContainer.GetConnectionString()
            });

            #endregion
        });

        return base.CreateHost(builder);
    }

    public override async ValueTask DisposeAsync()
    {
        await _azuriteContainer.DisposeAsync();
    }
}