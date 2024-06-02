using System;
using System.IO;
using System.Threading.Tasks;
using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using ManagedCode.Storage.FileSystem.Extensions;
using ManagedCode.Storage.FileSystem.Options;
using ManagedCode.Storage.Tests.Common.TestApp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.Azurite;
using Xunit;

namespace ManagedCode.Storage.Tests.Common;

[CollectionDefinition(nameof(StorageTestApplication))]
public class StorageTestApplication : WebApplicationFactory<HttpHostProgram>, ICollectionFixture<StorageTestApplication>
{
    private readonly AzuriteContainer _azuriteContainer;

    public StorageTestApplication()
    {
        _azuriteContainer = new AzuriteBuilder().WithImage(ContainerImages.Azurite)
            .Build();

        _azuriteContainer.StartAsync()
            .Wait();
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