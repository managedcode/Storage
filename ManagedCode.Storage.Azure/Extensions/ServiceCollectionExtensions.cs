using System;
using ManagedCode.Storage.Azure.Options;
using Microsoft.Extensions.DependencyInjection;

namespace ManagedCode.Storage.Azure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureStorage(
        this IServiceCollection serviceCollection,
        Action<AzureStorageOptions> action)
    {
        var azureStorageOptions = new AzureStorageOptions();
        action.Invoke(azureStorageOptions);

        return serviceCollection
            .AddScoped<IAzureStorage>(_ => new AzureStorage(azureStorageOptions));
    }
}