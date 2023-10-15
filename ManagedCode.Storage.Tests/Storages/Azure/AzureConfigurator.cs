using ManagedCode.Storage.Azure.Extensions;
using ManagedCode.Storage.Azure.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable MethodHasAsyncOverload

namespace ManagedCode.Storage.Tests.Storages.Azure;

public class AzureConfigurator
{
    public static ServiceProvider ConfigureServices(string connectionString)
    {

        var services = new ServiceCollection();

        services.AddAzureStorageAsDefault(opt =>
        {
            opt.Container = "managed-code-bucket";
            opt.ConnectionString = connectionString;
        });

        services.AddAzureStorage(new AzureStorageOptions
        {
            Container = "managed-code-bucket",
            ConnectionString = connectionString
        });
        return services.BuildServiceProvider();
    }
}