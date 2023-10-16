using ManagedCode.Storage.IntegrationTests.TestApp;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ManagedCode.Storage.IntegrationTests;

[CollectionDefinition(nameof(StorageTestApplication))]
public class StorageTestApplication : WebApplicationFactory<HttpHostProgram>, ICollectionFixture<StorageTestApplication>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            
        });
        
        return base.CreateHost(builder);
    }    
}